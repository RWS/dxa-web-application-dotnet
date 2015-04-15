using System.Collections.Generic;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Models
{
    [SemanticEntity(Vocab = "http://schema.org", EntityName = "ItemList", Prefix = "s", Public = true)]
    public class ContentList<T> : EntityBase
    {
        //TODO add concept of filtering/query (filter options and active filters/query)
        [SemanticProperty("s:headline")]
        public string Headline { get; set; }
        public Link Link { get; set; }
        public int PageSize { get; set; }
        public Tag ContentType { get; set; }
        public Tag Sort { get; set; }
        public int Start { get; set; }
        public int CurrentPage { get; set; }
        public bool HasMore { get; set; }
        [SemanticProperty("s:itemListElement")]
        public List<T> ItemListElements { get; set; }
        public ContentList()
        {
            ItemListElements = new List<T>();
            CurrentPage = 1;
        }
    }
}
