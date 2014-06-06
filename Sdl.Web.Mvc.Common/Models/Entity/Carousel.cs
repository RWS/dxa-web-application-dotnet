using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc.Models
{
    [SemanticEntity(Vocab = "http://schema.org", EntityName = "ItemList", Prefix = "s", Public = true)]
    public class ItemList : Entity
    {
        [SemanticProperty("s:itemListElement")]
        public List<Teaser> ItemListElements { get; set; }
    }
}
