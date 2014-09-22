using System.Collections.Generic;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Modules.Search
{
    [SemanticEntity(Vocab = "http://schema.org", EntityName = "ItemList", Prefix = "s", Public = true)]
    public class SearchResults<T> : EntityBase
    {
        [SemanticProperty("s:headline")]
        public string Headline { get; set; }
        public string ResultsText { get; set; }
        public string NoResultsText { get; set; }
        public Link Link { get; set; }
        public int PageSize { get; set; }
        public Tag SearchScope { get; set; }
        public Tag Sort { get; set; }
        public int Start { get; set; }
        public int CurrentPage { get; set; }
        public int Total { get; set; }
        public bool HasMore { get; set; }
        public QueryData Query { get; set; }
        [SemanticProperty("s:itemListElement")]
        public List<T> ItemListElements { get; set; }
        public SearchResults()
        {
            ItemListElements = new List<T>();
            CurrentPage = 1;
        }
    }
}
