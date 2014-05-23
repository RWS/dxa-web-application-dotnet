using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    public class Header
    {
        public Teaser Logo { get; set; }
        public Dictionary<string, Region> Regions { get; set; }

        public Header()
        {
            Regions = new Dictionary<string, Region>();
        }
    }
}