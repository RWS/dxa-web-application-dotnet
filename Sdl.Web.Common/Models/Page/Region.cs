using Sdl.Web.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Models
{
    public class Region : IRegion
    {
        public Dictionary<string, string> RegionData { get; set; }
        public string Module { get; set; }
        public string Name { get; set; }
        //Items are the raw entities that make up the page (eg Component Presentations, or other regions)
        public List<object> Items { get; set; }
        
        public Region()
        {
            Items = new List<object>();
        }
    }
}