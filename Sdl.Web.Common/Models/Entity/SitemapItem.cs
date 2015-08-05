using System;
using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    public class SitemapItem : EntityModel
    {
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
        public string Url { get; set; }
        public string Type { get; set; }
        public List<SitemapItem> Items { get; set; }
        public string PublishedDate { get; set; }
        public bool Visible { get; set; }
    }
}
