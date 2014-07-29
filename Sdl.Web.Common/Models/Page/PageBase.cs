using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Model for page content, used for includes and other 'non-concrete' pages. 
    /// Use WebPage for actual pages.
    /// </summary>
    public class PageBase : IPage
    {
        /// <summary>
        /// For storing system data (for example page id and modified date for xpm markup).
        /// </summary>
        public Dictionary<string, string> PageData { get; set; }
        public Dictionary<string, IRegion> Regions { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }

        public PageBase()
        {
            Regions = new Dictionary<string, IRegion>();
            PageData = new Dictionary<string, string>();
        }
    }
}