using System.Collections.Generic;
using Sdl.Web.Common.Models.Common;

namespace Sdl.Web.Common.Models.Entity
{
    [SemanticEntity(Vocab = "http://schema.org", EntityName = "ItemList", Prefix = "s", Public = true)]
    public class ItemList : EntityBase
    {
        [SemanticProperty("s:headline")]
        public string Headline { get; set; }
        [SemanticProperty("s:itemListElement")]
        public List<Teaser> ItemListElements { get; set; }
    }
}
