using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    public class Teaser
    {
        public Link Link { get; set; }
        public string Heading { get; set; }
        public MediaItem Media { get; set; }
        public string Text { get; set; }
    }
}