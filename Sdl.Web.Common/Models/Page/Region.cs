using System;
using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    public class Region : IRegion
    {
        public string Name { get; set; }
        /// <summary>
        /// Items are the raw entities that make up the page (eg Component Presentations, or other regions).
        /// </summary>
        public List<object> Items { get; set; }
        public Dictionary<string, string> RegionData { get; set; }
        public MvcData AppData { get; set; }
        [Obsolete("Please use AppData.AreaName.",true)]
        public string Module { get; set; }
        
        
        public Region()
        {
            Items = new List<object>();
        }
    }
}