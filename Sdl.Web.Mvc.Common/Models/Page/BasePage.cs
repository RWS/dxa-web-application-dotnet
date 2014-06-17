using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    /// <summary>
    /// Model for page content, used for includes and other 'non-concrete' pages. Use WebPage for actual pages
    /// </summary>
    public class BasePage
    {
        public Dictionary<string, Region> Regions { get; set; }
        public string Title { get; set; }
        public BasePage()
        {
            Regions = new Dictionary<string, Region>();
        }
    }
}