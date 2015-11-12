using Sdl.Web.Common;
using Sdl.Web.Common.Models;
using System;
using System.Collections.Generic;
using Tridion.ContentDelivery.DynamicContent.Query;
using Tridion.ContentDelivery.Meta;
using Tridion.ContentDelivery.Taxonomies;

namespace Sdl.Web.Tridion.Query
{
    public class BrokerQuery
    {
        public int SchemaId { get; set; }
        public int PublicationId { get; set; }
        public int MaxResults { get; set; }
        public string Sort { get; set; }
        public int Start { get; set; }
        public int PageSize { get; set; }
        public Dictionary<string, List<string>> KeywordFilters { get; set; }
        public bool HasMore { get; set; }

        public IEnumerable<Teaser> ExecuteQuery()
        {
            Criteria criteria = BuildCriteria();
            global::Tridion.ContentDelivery.DynamicContent.Query.Query query = new global::Tridion.ContentDelivery.DynamicContent.Query.Query(criteria);
            if (!String.IsNullOrEmpty(Sort) && Sort.ToLower() != "none")
            {
                query.AddSorting(GetSortParameter());
            }
            if (MaxResults > 0)
            {
                query.SetResultFilter(new LimitFilter(MaxResults));
            }
            if (PageSize > 0)
            {
                //We set the page size to one more than what we need, to see if there are more pages to come...
                query.SetResultFilter(new PagingFilter(Start, PageSize + 1));
            }
            try
            {
                ComponentMetaFactory componentMetaFactory = new ComponentMetaFactory(PublicationId);
                List<Teaser> results = new List<Teaser>();
                string[] ids = query.ExecuteQuery();
                HasMore = ids.Length > PageSize;
                int count = 0;
                foreach (string compId in ids)
                {
                    if (count >= PageSize)
                    {
                        break;
                    }
                    IComponentMeta compMeta = componentMetaFactory.GetMeta(compId);
                    if (compMeta != null)
                    {
                        results.Add(GetTeaserFromMeta(compMeta));
                    }
                    count++;
                }
                return results;
            }
            catch (Exception ex)
            {
                throw new DxaException("Error executing Broker Query", ex);
            }
        }

        private static Teaser GetTeaserFromMeta(IComponentMeta compMeta)
        {
            Teaser result = new Teaser
                {
                    Link = new Link { Url = String.Format("tcm:{0}-{1}", compMeta.PublicationId, compMeta.Id) },
                    Date = GetDateFromCustomMeta(compMeta.CustomMeta, "dateCreated") ?? compMeta.LastPublicationDate,
                    Headline = GetTextFromCustomMeta(compMeta.CustomMeta, "name") ?? compMeta.Title,
                    Text = GetTextFromCustomMeta(compMeta.CustomMeta, "introText")
                };
            return result;
        }

        private static string GetTextFromCustomMeta(CustomMeta meta, string fieldname)
        {
            if (meta.NameValues.Contains(fieldname))
            {
                object value = meta.GetValue(fieldname);
                return (value == null) ? null : value.ToString();
            }
            return null;
        }

        private static DateTime? GetDateFromCustomMeta(CustomMeta meta, string fieldname)
        {
            if (meta.NameValues.Contains(fieldname))
            {
                return meta.GetValue(fieldname) as DateTime?;
            }
            return null;
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
            if (SchemaId > 0)
            {
                children.Add(new ItemSchemaCriteria(SchemaId));
            }
            if (PublicationId > 0)
            {
                children.Add(new PublicationCriteria(PublicationId));
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
            SortDirection dir = Sort.ToLower().EndsWith("asc") ? SortParameter.Ascending : SortParameter.Descending;
            return new SortParameter(GetSortColumn(), dir);
        }

        private SortColumn GetSortColumn()
        {
            //TODO add more options if required
            int pos = Sort.Trim().IndexOf(" ", StringComparison.Ordinal);
            string sort = pos > 0 ? Sort.Trim().Substring(0, pos) : Sort.Trim();
            switch (sort.ToLower())
            {
                case "title":
                    return SortParameter.ItemTitle;
                case "pubdate":
                    return SortParameter.ItemLastPublishedDate;
                default:
                    //Default is to assume that its a custom metadata date field;
                    return new CustomMetaKeyColumn(Sort, MetadataType.DATE);
            }
        }
    }
}
