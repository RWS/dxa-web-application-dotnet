using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    public class Header
    {
        public Image Logo { get; set; }
        public Link LogoLink { get; set; }
        public string Heading { get; set; }
        public string Subheading { get; set; }
        public Dictionary<string, Region> Regions { get; set; }

        public Header()
        {
            Regions = new Dictionary<string, Region>();
        }
    }
}