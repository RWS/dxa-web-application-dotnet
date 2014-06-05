using Sdl.Web.Mvc.Common;
using Sdl.Web.Mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Tridion.ContentDelivery.DynamicContent;
using Tridion.ContentDelivery.DynamicContent.Query;
using Tridion.ContentDelivery.Taxonomies;

namespace Sdl.Web.Tridion
{
    public class ContentQuery
    {
        public IContentProvider ContentProvider{ get; set; }
        public int SchemaId { get; set; }
        public int PublicationId { get; set; }
        public int MaxResults { get; set; }
        public string ComponentTemplateUri { get; set; }
        public string Sort { get; set; }
        public int Start { get; set; }
        public int PageSize { get; set; }
        public Dictionary<string, List<string>> KeywordFilters { get; set; }

        public List<Teaser> ExecuteQuery()
        {
            Criteria criteria = BuildCriteria();
            Query query = new Query(criteria);
            if (!String.IsNullOrEmpty(Sort))
            {
                query.AddSorting(GetSortParameter());
            }
            if (MaxResults > 0)
            {
                query.SetResultFilter(new LimitFilter(MaxResults));
            }
            if (PageSize > 0)
            {
                query.SetResultFilter(new PagingFilter(Start, PageSize));
            }
            try
            {
                var items = query.ExecuteQuery();
                var results = new List<Teaser>();
                foreach (var item in query.ExecuteEntityQuery())
                {
                    var result = new Teaser();
                    result.Headline = item.Title;
                    result.Link = new Link { Url = ContentProvider.ProcessUrl(String.Format("tcm:{0}-{1}", item.PublicationId, item.Id)) };
                    results.Add(result);
                }
                return results;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error running broker query: {0}.", ex.Message ), ex);
            }
        }

        /// <summary>
        /// Sets the keyword filters using a list of keyword uri strings
        /// </summary>
        /// <param name="encodedFilters"></param>
        public void SetKeywordFilters(List<String> keywordUris)
        {
            var taxonomyFactory = new TaxonomyFactory();
            List<Keyword> keywords = new List<Keyword>();
            foreach (var kwUri in keywordUris)
            {
                var kw = taxonomyFactory.GetTaxonomyKeyword(kwUri);
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
        /// <param name="encodedFilters"></param>
        public void SetKeywordFilters(List<Keyword> keywords)
        {
            if (KeywordFilters == null)
            {
                KeywordFilters = new Dictionary<string, List<string>>();
            }
            foreach (var kw in keywords)
            {
                var taxonomy = kw.TaxonomyUri;
                if (!KeywordFilters.ContainsKey(taxonomy))
                {
                    KeywordFilters.Add(taxonomy, new List<string>());
                }
                KeywordFilters[taxonomy].Add(kw.KeywordUri);
            }
        }

        public static Keyword LoadKeyword(string keywordUri)
        {
            var taxonomyFactory = new TaxonomyFactory();
            return taxonomyFactory.GetTaxonomyKeyword(keywordUri);
        }

        /// <summary>
        /// Gets a list of keyword objects based on their URIs
        /// </summary>
        /// <param name="keywordUris"></param>
        /// <returns></returns>
        public static List<Keyword> LoadKeywords(List<string> keywordUris)
        {
            var res = new List<Keyword>();
            var taxonomyFactory = new TaxonomyFactory();
            foreach (var uri in keywordUris)
            {
                var kw = taxonomyFactory.GetTaxonomyKeyword(uri);
                if (kw != null)
                {
                    res.Add(kw);
                }
            }
            return res;
        }

        private Criteria BuildCriteria()
        {
            var children = new List<Criteria>();
            children.Add(new ItemTypeCriteria(16));
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
                foreach (var taxonomy in KeywordFilters.Keys)
                {
                    foreach (var keyword in KeywordFilters[taxonomy])
                    {
                        children.Add(new TaxonomyKeywordCriteria(taxonomy, keyword, true));
                    }
                }
            }
            return new AndCriteria(children.ToArray());
        }

        private string SerializeQuery()
        {
            var sw = new System.IO.StringWriter();
            var serializer = new XmlSerializer(this.GetType());
            serializer.Serialize(sw, this);
            return sw.ToString();
        }

        private SortParameter GetSortParameter()
        {
            var dir = Sort.ToLower().EndsWith("asc") ? SortParameter.Ascending : SortParameter.Descending;
            return new SortParameter(GetSortColumn(), dir);
        }

        private SortColumn GetSortColumn()
        {
            //TODO add more options if required
            var sort = Sort.ToLower().Trim();
            var pos = Sort.Trim().IndexOf(" ");
            sort = pos > 0 ? Sort.Trim().Substring(0, pos) : Sort.Trim();
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
