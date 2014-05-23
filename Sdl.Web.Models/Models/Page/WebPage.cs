using System.Collections.Generic;

namespace Sdl.Web.Mvc.Models
{
    /// <summary>
    /// Model for the data that is used to render a web page
    /// </summary>
    public class WebPage
    {
        public string Url { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public Breadcrumb Breadcrumb { get; set; }
        //TODO: Are dictionaries better than explicit values? Somethings you may always have (description etc.)
        public Dictionary<string, string> Meta { get; set; }
        public Dictionary<string, Region> Regions { get; set; }
        public Header Header { get; set; }
        public Footer Footer { get; set; }
        
        /*Other stuff to consider:
         1. Specific css/js for the page based on its components/plugins
         2. Analytics variables
         3. Parent SG id(s) for building navigation state - could be part of breadcrumb
         */
        public WebPage()
        {
            Meta = new Dictionary<string, string>();
            Regions = new Dictionary<string, Region>();
        }
    }
}