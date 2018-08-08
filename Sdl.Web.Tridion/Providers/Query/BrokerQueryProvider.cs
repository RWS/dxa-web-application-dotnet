using System;
using System.Collections.Generic;
using System.Linq;
using Sdl.Web.Common;
using Sdl.Web.Common.Models;
using Tridion.ContentDelivery.DynamicContent.Query;
using Tridion.ContentDelivery.Taxonomies;

namespace Sdl.Web.Tridion.Providers.Query
{
    public class BrokerQueryProvider : Common.Interfaces.IQueryProvider
    {
        public Dictionary<string, List<string>> KeywordFilters { get; set; }
        public bool HasMore { get; set; }

        public IEnumerable<string> ExecuteQuery(SimpleBrokerQuery queryParams)
        {
            Criteria criteria = BuildCriteria(queryParams);
            global::Tridion.ContentDelivery.DynamicContent.Query.Query query = new global::Tridion.ContentDelivery.DynamicContent.Query.Query(criteria);
            if (!string.IsNullOrEmpty(queryParams.Sort) && queryParams.Sort.ToLower() != "none")
            {
                query.AddSorting(GetSortParameter(queryParams));
            }
            if (queryParams.MaxResults > 0)
            {
                query.SetResultFilter(new LimitFilter(queryParams.MaxResults));
            }

            int pageSize = queryParams.PageSize;
            if (pageSize > 0)
            {
                //We set the page size to one more than what we need, to see if there are more pages to come...
                query.SetResultFilter(new PagingFilter(queryParams.Start, pageSize + 1));
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
            List<Keyword> keywords = keywordUris.Select(kwUri => taxonomyFactory.GetTaxonomyKeyword(kwUri)).Where(kw => kw != null).ToList();
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
            TaxonomyFactory taxonomyFactory = new TaxonomyFactory();
            return keywordUris.Select(uri => taxonomyFactory.GetTaxonomyKeyword(uri)).Where(kw => kw != null).ToList();
        }

        private Criteria BuildCriteria(SimpleBrokerQuery queryParams)
        {
            List<Criteria> children = new List<Criteria> { new ItemTypeCriteria(16) };
            if (queryParams.SchemaId > 0)
            {
                children.Add(new ItemSchemaCriteria(queryParams.SchemaId));
            }
            if (queryParams.PublicationId > 0)
            {
                children.Add(new PublicationCriteria(queryParams.PublicationId));
            }
            if (KeywordFilters != null)
            {
                children.AddRange((from taxonomy in KeywordFilters.Keys from keyword in KeywordFilters[taxonomy] select new TaxonomyKeywordCriteria(taxonomy, keyword, true)).Cast<Criteria>());
            }
            return new AndCriteria(children.ToArray());
        }

        private SortParameter GetSortParameter(SimpleBrokerQuery queryParams)
        {
            SortDirection dir = queryParams.Sort.ToLower().EndsWith("asc") ? SortParameter.Ascending : SortParameter.Descending;
            return new SortParameter(GetSortColumn(queryParams), dir);
        }

        private SortColumn GetSortColumn(SimpleBrokerQuery queryParams)
        {
            //TODO add more options if required
            int pos = queryParams.Sort.Trim().IndexOf(" ", StringComparison.Ordinal);
            string sort = pos > 0 ? queryParams.Sort.Trim().Substring(0, pos) : queryParams.Sort.Trim();
            switch (sort.ToLower())
            {
                case "title":
                    return SortParameter.ItemTitle;
                case "pubdate":
                    return SortParameter.ItemLastPublishedDate;
                default:
                    //Default is to assume that its a custom metadata date field;
                    return new CustomMetaKeyColumn(queryParams.Sort, MetadataType.DATE);
            }
        }
    }
}
