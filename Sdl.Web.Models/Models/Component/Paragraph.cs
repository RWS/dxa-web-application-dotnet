using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    public class Paragraph
    {
        public string Subheading { get; set; }
        public string Text { get; set; }
        public MediaItem Media { get; set; }
        public string Caption { get; set; }
    }
}