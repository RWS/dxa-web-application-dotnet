using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    public class Footer
    {
        public LinkList LinkList { get; set; }
        public Dictionary<string, Region> Regions { get; set; }
        
        public Footer()
        {
            Regions = new Dictionary<string, Region>();
        }
    }
}