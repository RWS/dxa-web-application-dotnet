using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Models
{
    [SemanticEntity("http://schema.org", "Thing", "s")]
    public class Image : MediaItem
    {
        [SemanticProperty("s:name")]
        public string AlternateText { get; set; }
    }
}