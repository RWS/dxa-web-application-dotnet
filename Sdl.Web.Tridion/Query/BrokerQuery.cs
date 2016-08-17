using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;
using Sdl.Web.Tridion.Mapping;
using System;
using System.Collections;
using System.Collections.Generic;
using Tridion.ContentDelivery.DynamicContent;
using Tridion.ContentDelivery.DynamicContent.Query;
using Tridion.ContentDelivery.Meta;
using Tridion.ContentDelivery.Taxonomies;

namespace Sdl.Web.Tridion.Query
{
    public class BrokerQuery
    {
        public Dictionary<string, List<string>> KeywordFilters { get; set; }
        public bool HasMore { get; set; }

        public IEnumerable<EntityModel> ExecuteQuery(Type modelType, SimpleBrokerQuery qParams)
        {
            Criteria criteria = BuildCriteria(qParams);
            global::Tridion.ContentDelivery.DynamicContent.Query.Query query = new global::Tridion.ContentDelivery.DynamicContent.Query.Query(criteria);
            if (!String.IsNullOrEmpty(qParams.Sort) && qParams.Sort.ToLower() != "none")
            {
                query.AddSorting(GetSortParameter(qParams));
            }
            if (qParams.MaxResults > 0)
            {
                query.SetResultFilter(new LimitFilter(qParams.MaxResults));
            }
            if (qParams.PageSize > 0)
            {
                //We set the page size to one more than what we need, to see if there are more pages to come...
                query.SetResultFilter(new PagingFilter(qParams.Start, qParams.PageSize + 1));
            }
            try
            {
                List<EntityModel> models = new List<EntityModel>();
                string[] ids = query.ExecuteQuery();
                if (ids != null && ids.Length > 0)
                {
                    ComponentMetaFactory componentMetaFactory = new ComponentMetaFactory(qParams.PublicationId);

                    for (int i = 0; i < ids.Length && models.Count < qParams.PageSize; i++)
                    {
                        IComponentMeta componentMeta = componentMetaFactory.GetMeta(ids[i]);
                        EntityModel model = ModelBuilderPipeline.CreateEntityModel(componentMeta, modelType, qParams.Localization);
                        if (model != null)
                        {
                            models.Add(model);
                        }
                    }
                    HasMore = ids.Length > models.Count;
                }
                return models;
            }
            catch (Exception ex)
            {
                throw new DxaException("Error executing Broker Query", ex);
            }
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

        private Criteria BuildCriteria(SimpleBrokerQuery qParams)
        {
            List<Criteria> children = new List<Criteria> { new ItemTypeCriteria(16) };
            if (qParams.SchemaId > 0)
            {
                children.Add(new ItemSchemaCriteria(qParams.SchemaId));
            }
            if (qParams.PublicationId > 0)
            {
                children.Add(new PublicationCriteria(qParams.PublicationId));
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

        private SortParameter GetSortParameter(SimpleBrokerQuery qParams)
        {
            SortDirection dir = qParams.Sort.ToLower().EndsWith("asc") ? SortParameter.Ascending : SortParameter.Descending;
            return new SortParameter(GetSortColumn(qParams), dir);
        }

        private SortColumn GetSortColumn(SimpleBrokerQuery qParams)
        {
            //TODO add more options if required
            int pos = qParams.Sort.Trim().IndexOf(" ", StringComparison.Ordinal);
            string sort = pos > 0 ? qParams.Sort.Trim().Substring(0, pos) : qParams.Sort.Trim();
            switch (sort.ToLower())
            {
                case "title":
                    return SortParameter.ItemTitle;
                case "pubdate":
                    return SortParameter.ItemLastPublishedDate;
                default:
                    //Default is to assume that its a custom metadata date field;
                    return new CustomMetaKeyColumn(qParams.Sort, MetadataType.DATE);
            }
        }
    }
}
