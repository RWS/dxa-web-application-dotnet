using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    public class LinkGroup
    {
        //TODO - maybe have some context information (like applicability for device types)
        public string Heading { get; set; }
        public MediaItem Media { get; set; }
        public List<Link> Links { get; set; }
    }
}