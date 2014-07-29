using System;
using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    public class SitemapItem : EntityBase
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
            set { _url = ProcessUrl(value); }
        }

        private static string ProcessUrl(string value)
        {
            return value.Replace(".html", String.Empty);
        }

        public string Type { get; set; }
        public List<SitemapItem> Items { get; set; }
        public DateTime PublishedDate { get; set; }
        public bool Visible { get; set; }
    }
}
