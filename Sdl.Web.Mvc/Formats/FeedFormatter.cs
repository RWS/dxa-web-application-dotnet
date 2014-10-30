using Sdl.Web.Common.Models;
using System.ServiceModel.Syndication;
using System.Web.Mvc;
using System.Linq;
using System.Collections.Generic;
using System;
using Sdl.Web.Mvc.Configuration;
using System.Web;
using System.Collections.Specialized;

namespace Sdl.Web.Mvc.Formats
{
    public abstract class FeedFormatter : BaseFormatter
    {
        protected SyndicationFeed GetData(object model)
        {
            var page = model as WebPage;
            if (page!=null)
            {
                var description = page.Meta.ContainsKey("description") ? page.Meta["description"] : null;
                var url = RemoveFormatFromUrl();
                SyndicationFeed feed = new SyndicationFeed(page.Title, description, new Uri(url));
                feed.Language = WebRequestContext.Localization.Culture;
                feed.Items = GetFeedItemsFromPage(page);
                return feed;
            }
            return null;
        }

        private string RemoveFormatFromUrl()
        {
            var filtered = HttpUtility.ParseQueryString(HttpContext.Current.Request.QueryString.ToString());
            filtered.Remove("format");
            return HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path) + (filtered.Count>0 ?  "?" + filtered : "");
        }

        private List<SyndicationItem> GetFeedItemsFromPage(WebPage page)
        {
            var items = new List<SyndicationItem>();
            foreach (var region in page.Regions.Values)
            {
                foreach (var entity in region.Items)
                {
                    if (entity is Teaser)
                    {
                        var teaser = entity as Teaser;
                        items.Add(new SyndicationItem(teaser.Headline, teaser.Text, new Uri(teaser.Link.Url)));
                    }
                }
            }
            return items;
        }
    }
}
