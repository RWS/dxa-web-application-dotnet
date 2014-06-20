using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    [SemanticEntity("http://schema.org", "Thing", "s")]
    public class Download : MediaItem
    {
        [SemanticProperty("s:name")]
        [SemanticProperty("s:description")]
        public string Description { get; set; }
    }
}

 