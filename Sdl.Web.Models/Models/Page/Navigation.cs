using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc.Models
{

    public class Navigation
    {
        private SitemapItem rootFolder { get; set; }

        public Navigation()
        {
            rootFolder = new SitemapItem("");
        }
    }
    public class SitemapItem
    {
        private string _url;

        public SitemapItem()
        {
            Items = new List<SitemapItem>();
        }

        public SitemapItem(String title)
        {
            Items = new List<SitemapItem>();
            Title = title;
        }

        public string Title { get; set; }

        public string Url
        {
            get { return _url; }
            set { _url = RemoveNonRequiredExtensions(value); }
        }

        private string RemoveNonRequiredExtensions(string value)
        {
            return value.Replace(".html", "");
        }

        public string Id { get; set; }
        public string Type { get; set; }
        public List<SitemapItem> Items { get; set; }
        public DateTime PublishedDate { get; set; }
    }
}
