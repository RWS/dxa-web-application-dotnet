using System.Collections.Generic;

namespace Sdl.Web.Mvc.Models
{
    /// <summary>
    /// Model for the data that is used to render a web page
    /// </summary>
    public class WebPage : BasePage
    {
        public string Url { get; set; }
        public Dictionary<string, string> Meta { get; set; }
        
        //Included content not explicitly added to the page but required for rendering (header, footer, nav etc.)
        public Dictionary<string, BasePage> Includes { get; set; }
        
        public WebPage()
        {
            PageData = new Dictionary<string, string>();
            Meta = new Dictionary<string, string>();
            Regions = new Dictionary<string, Region>();
            Includes = new Dictionary<string, BasePage>();
        }
    }
}