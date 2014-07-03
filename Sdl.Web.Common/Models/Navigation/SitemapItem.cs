using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Models
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

        private string ProcessUrl(string value)
        {
            return value.Replace(".html", "");
        }

        public string Type { get; set; }
        public List<SitemapItem> Items { get; set; }
        public DateTime PublishedDate { get; set; }
        public bool Visible { get; set; }
    }
}
