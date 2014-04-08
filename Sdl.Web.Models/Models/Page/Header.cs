using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    public class Header
    {
        public Image Logo { get; set; }
        public string Heading { get; set; }
        public string Subheading { get; set; }
        public List<Link> Links { get; set; }
        //TODO: rethink how other widgets are added to header (like navigation etc.)
        public Dictionary<string, Region> Regions { get; set; }
        
    }
}