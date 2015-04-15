using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Model for the data that is used to render a web page.
    /// </summary>
    public class WebPage : PageBase
    {
        public string Url { get; set; }
        public Dictionary<string, string> Meta { get; set; }
        
        /// <summary>
        /// Included content not explicitly added to the page but required for rendering (header, footer, nav etc.).
        /// </summary>
        public Dictionary<string, IPage> Includes { get; set; }
        
        public WebPage()
        {
            PageData = new Dictionary<string, string>();
            Meta = new Dictionary<string, string>();
            Regions = new Dictionary<string, IRegion>();
            Includes = new Dictionary<string, IPage>();
        }
    }
}