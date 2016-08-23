using Sdl.Web.Common;
using Sdl.Web.Common.Models;
using Sdl.Web.Tridion.Mapping;
using System;
using System.Collections;
using System.Collections.Generic;
using Tridion.ContentDelivery.DynamicContent.Query;
using Tridion.ContentDelivery.Meta;
using Tridion.ContentDelivery.Taxonomies;

namespace Sdl.Web.Tridion.Query
{
    internal class BrokerQuery
    {
        private readonly SimpleBrokerQuery _queryParameters;

        internal Dictionary<string, List<string>> KeywordFilters { get; set; }
        internal bool HasMore { get; set; }

        internal BrokerQuery(SimpleBrokerQuery queryParameters)
        {
            _queryParameters = queryParameters;
        }

        internal IEnumerable<EntityModel> ExecuteQuery(Type resultType)
        {
            Criteria criteria = BuildCriteria();
            global::Tridion.ContentDelivery.DynamicContent.Query.Query query = new global::Tridion.ContentDelivery.DynamicContent.Query.Query(criteria);
            if (!string.IsNullOrEmpty(_queryParameters.Sort) && _queryParameters.Sort.ToLower() != "none")
            {
                query.AddSorting(GetSortParameter());
            }
            if (_queryParameters.MaxResults > 0)
            {
                query.SetResultFilter(new LimitFilter(_queryParameters.MaxResults));
            }
            if (_queryParameters.PageSize > 0)
            {
                //We set the page size to one more than what we need, to see if there are more pages to come...
                query.SetResultFilter(new PagingFilter(_queryParameters.Start, _queryParameters.PageSize + 1));
            }
            try
            {
                List<EntityModel> models = new List<EntityModel>();

                string[] componentIds = query.ExecuteQuery();
                if (componentIds == null || componentIds.Length == 0)
                {
                    return models;
                }

                ComponentMetaFactory componentMetaFactory = new ComponentMetaFactory(_queryParameters.PublicationId);
                int pageSize = _queryParameters.PageSize;
                int count = 0;
                foreach (string componentId in componentIds)
                {
                    IComponentMeta componentMeta = componentMetaFactory.GetMeta(componentId);
                    DD4T.ContentModel.IComponent dd4tComponent = CreateComponent(componentMeta);
                    EntityModel model = ModelBuilderPipeline.CreateEntityModel(dd4tComponent, resultType, _queryParameters.Localization);
                    models.Add(model);
                    if (++count == pageSize)
                    {
                        break;
                    }
                }
                HasMore = componentIds.Length > count;
                return models;
            }
            catch (Exception ex)
            {
                throw new DxaException("Error executing Broker Query", ex);
            }
        }

        /// <summary>
        /// Creates a lightweight DD4T Component that contains enough information such that the semantic model builder can cope and build a strongly typed model from it.
        /// </summary>
        /// <param name="componentMeta">A <see cref="IComponentMeta"/> instance obtained from CD API.</param>
        /// <returns>A DD4T Component.</returns>
        private static DD4T.ContentModel.IComponent CreateComponent(IComponentMeta componentMeta)
        {
            DD4T.ContentModel.Component component = new DD4T.ContentModel.Component
            {
                Id = string.Format("tcm:{0}-{1}", componentMeta.PublicationId, componentMeta.Id),
                LastPublishedDate = componentMeta.LastPublicationDate,
                RevisionDate = componentMeta.ModificationDate,
                Schema = new DD4T.ContentModel.Schema
                {
                    PublicationId = componentMeta.PublicationId.ToString(),
                    Id = string.Format("tcm:{0}-{1}", componentMeta.PublicationId, componentMeta.SchemaId)
                },
                MetadataFields = new DD4T.ContentModel.FieldSet()
            };

            DD4T.ContentModel.FieldSet metadataFields = new DD4T.ContentModel.FieldSet();
            component.MetadataFields.Add("standardMeta", new DD4T.ContentModel.Field { EmbeddedValues = new List<DD4T.ContentModel.FieldSet> { metadataFields } });
            foreach (DictionaryEntry de in componentMeta.CustomMeta.NameValues)
            {
                object v = ((NameValuePair)de.Value).Value;
                if (v != null)
                {
                    string k = de.Key.ToString();
                    metadataFields.Add(k, new DD4T.ContentModel.Field
                    {
                        Name = k,
                        Values = new List<string> { v.ToString() }
                    });
                }
            }

            // The semantic mapping requires that some metadata fields exist. This may not be the case so we map some component meta properties onto them
            // if they don't exist.
            if (!metadataFields.ContainsKey("dateCreated"))
            {
                metadataFields.Add("dateCreated", new DD4T.ContentModel.Field { Name = "dateCreated", Values = new List<string> { componentMeta.LastPublicationDate.ToString() } });
            }

            if (!metadataFields.ContainsKey("name"))
            {
                metadataFields.Add("name", new DD4T.ContentModel.Field { Name = "name", Values = new List<string> { componentMeta.Title } });
            }

            return component;
        }

        /// <summary>
        /// Sets the keyword filters using a list of keyword uri strings
        /// </summary>
        /// <param name="keywordUris"></param>
        public void SetKeywordFilters(List<String> keywordUris)
        {
            TaxonomyFactory taxonomyFactory = new TaxonomyFactory();
            List<Keyword> keywords = new List<Keyword>();
            foreach (string kwUri in keywordUris)
            {
                Keyword kw = taxonomyFactory.GetTaxonomyKeyword(kwUri);
                if (kw != null)
                {
                    keywords.Add(kw);
                }
            }
            SetKeywordFilters(keywords);
        }

        /// <summary>
        /// Sets the keyword filters using a list of keyword objects
        /// </summary>
        /// <param name="keywords"></param>
        public void SetKeywordFilters(List<Keyword> keywords)
        {
            if (KeywordFilters == null)
            {
                KeywordFilters = new Dictionary<string, List<string>>();
            }
            foreach (Keyword kw in keywords)
            {
                string taxonomy = kw.TaxonomyUri;
                if (!KeywordFilters.ContainsKey(taxonomy))
                {
                    KeywordFilters.Add(taxonomy, new List<string>());
                }
                KeywordFilters[taxonomy].Add(kw.KeywordUri);
            }
        }

        public static Keyword LoadKeyword(string keywordUri)
        {
            TaxonomyFactory taxonomyFactory = new TaxonomyFactory();
            return taxonomyFactory.GetTaxonomyKeyword(keywordUri);
        }

        /// <summary>
        /// Gets a list of keyword objects based on their URIs
        /// </summary>
        /// <param name="keywordUris"></param>
        /// <returns></returns>
        public static List<Keyword> LoadKeywords(List<string> keywordUris)
        {
            List<Keyword> res = new List<Keyword>();
            TaxonomyFactory taxonomyFactory = new TaxonomyFactory();
            foreach (string uri in keywordUris)
            {
                Keyword kw = taxonomyFactory.GetTaxonomyKeyword(uri);
                if (kw != null)
                {
                    res.Add(kw);
                }
            }
            return res;
        }

        private Criteria BuildCriteria()
        {
            List<Criteria> children = new List<Criteria> { new ItemTypeCriteria(16) };
            if (_queryParameters.SchemaId > 0)
            {
                children.Add(new ItemSchemaCriteria(_queryParameters.SchemaId));
            }
            if (_queryParameters.PublicationId > 0)
            {
                children.Add(new PublicationCriteria(_queryParameters.PublicationId));
            }
            if (KeywordFilters != null)
            {
                foreach (string taxonomy in KeywordFilters.Keys)
                {
                    foreach (string keyword in KeywordFilters[taxonomy])
                    {
                        children.Add(new TaxonomyKeywordCriteria(taxonomy, keyword, true));
                    }
                }
            }
            return new AndCriteria(children.ToArray());
        }

        private SortParameter GetSortParameter()
        {
            SortDirection dir = _queryParameters.Sort.ToLower().EndsWith("asc") ? SortParameter.Ascending : SortParameter.Descending;
            return new SortParameter(GetSortColumn(), dir);
        }

        private SortColumn GetSortColumn()
        {
            //TODO add more options if required
            int pos = _queryParameters.Sort.Trim().IndexOf(" ", StringComparison.Ordinal);
            string sort = pos > 0 ? _queryParameters.Sort.Trim().Substring(0, pos) : _queryParameters.Sort.Trim();
            switch (sort.ToLower())
            {
                case "title":
                    return SortParameter.ItemTitle;
                case "pubdate":
                    return SortParameter.ItemLastPublishedDate;
                default:
                    //Default is to assume that its a custom metadata date field;
                    return new CustomMetaKeyColumn(_queryParameters.Sort, MetadataType.DATE);
            }
        }
    }
}
