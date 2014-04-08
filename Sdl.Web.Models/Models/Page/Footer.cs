using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    public class Footer
    {
        public string Copyright { get; set; }
        public Image Logo { get; set; }
        public List<Link> Links { get; set; }
        //TODO: rethink how other widgets are added to footer (like link groups etc.)
        public Dictionary<string, Region> Regions { get; set; }
    }
}