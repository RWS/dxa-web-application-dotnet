using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    public class Paragraph : EntityBase
    {
        public string Subheading { get; set; }
        public string Content { get; set; }
        public MediaItem Media { get; set; }
        public string Caption { get; set; }
    }
}