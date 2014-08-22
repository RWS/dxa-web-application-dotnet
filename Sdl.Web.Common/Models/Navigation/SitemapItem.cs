using System;
using System.Collections.Generic;
using System.IO;

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
            return Path.HasExtension(value) ? value.Substring(0, value.Length - Path.GetExtension(value).Length) : value;
        }

        public string Type { get; set; }
        public List<SitemapItem> Items { get; set; }
        public string PublishedDate { get; set; }
        public bool Visible { get; set; }
    }
}
