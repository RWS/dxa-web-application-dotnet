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
        
        
        // TODO: do we need this public constructor?
        public Region()
        {
            Items = new List<object>();
        }

        /// <summary>
        /// Creates an empty Region
        /// </summary>
        /// <param name="name">The name of the Region.</param>
        /// <param name="viewName">The name of the View to use to render the Region.</param>
        public Region(string name, string viewName) 
            : this()
        {
            Name = name;
            AppData = new MvcData
            {
                ViewName = viewName
            };
        }
    }
}