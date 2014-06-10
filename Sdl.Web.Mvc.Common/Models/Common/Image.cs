using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    [SemanticEntity("http://schema.org", "Thing", "s")]
    public class Image : MediaItem
    {
        public string Url { get; set; }
        [SemanticProperty("s:name")]
        public string AlternateText { get; set; }
        public int FileSize { get; set; }
        
        //TODO: alternate formats
    }
}