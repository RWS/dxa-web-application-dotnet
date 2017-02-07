using Sdl.Web.Common;
using Sdl.Web.Common.Models;
using Sdl.Web.Tridion.Mapping;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        internal IEnumerable<string> ExecuteQuery()
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

            int pageSize = _queryParameters.PageSize;
            if (pageSize > 0)
            {
                //We set the page size to one more than what we need, to see if there are more pages to come...
                query.SetResultFilter(new PagingFilter(_queryParameters.Start, pageSize + 1));
            }
            try
            {
                string[] componentIds = query.ExecuteQuery();
                if (componentIds == null)
                {
                    return new string[0];
                }

                HasMore = componentIds.Length > pageSize;

                return (pageSize > 0) ? componentIds.Take(pageSize) : componentIds;
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
