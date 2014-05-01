using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    public class Footer
    {
        public string Copyright { get; set; }
        public List<Link> Links { get; set; }
        public Dictionary<string, Region> Regions { get; set; }
        
        public Footer()
        {
            Links = new List<Link>();
            Regions = new Dictionary<string, Region>();
        }
    }
}