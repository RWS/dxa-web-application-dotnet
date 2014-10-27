using System.Collections.Generic;
using Sdl.Web.Common.Models;
using System.Collections.Specialized;

namespace Sdl.Web.Modules.Search
{
    [SemanticEntity(Vocab = "http://schema.org", EntityName = "ItemList", Prefix = "s", Public = true)]
    public class SearchQuery<T> : EntityBase
    {
        //Content from CMS
        [SemanticProperty("s:headline")]
        public string Headline { get; set; }
        public string ResultsText { get; set; }
        public string NoResultsText { get; set; }
        public int PageSize { get; set; }
        //Content from parameters
        public int Start { get; set; }
        public int CurrentPage { get; set; }
        public string QueryText { get; set; }
        public NameValueCollection Parameters { get; set; }
        //Content from query
        public int Total { get; set; }
        public bool HasMore { get; set; }
        [SemanticProperty("s:itemListElement")]
        public List<T> Results { get; set; }
        public SearchQuery()
        {
            Results = new List<T>();
            CurrentPage = 1;
            PageSize = 10;
        }
    }
}
