using System.Collections.Specialized;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using System;
using System.ServiceModel.Syndication;
using System.Web;

namespace Sdl.Web.Mvc.Formats
{
    /// <summary>
    /// Abstract base class for syndication feed formatters.
    /// </summary>
    public abstract class FeedFormatter : BaseFormatter
    {
        /// <summary>
        /// Extracts a syndication feed from a given View Model.
        /// </summary>
        /// <param name="pageModel">The Page Model to extract the feed from.</param>
        /// <returns>The extracted syndication feed.</returns>
        protected SyndicationFeed ExtractSyndicationFeed(PageModel pageModel)
        {
            if (pageModel == null)
            {
                return null;
            }

            string description = null;
            if (pageModel.Meta != null)
            {
                pageModel.Meta.TryGetValue("description", out description);
            }
            
            string feedAlternateLink = GetPageUrlWithoutFormatParameter();

            return new SyndicationFeed(pageModel.Title, description, new Uri(feedAlternateLink))
            {
                Language = WebRequestContext.Localization.Culture,
                Items = pageModel.ExtractSyndicationFeedItems(WebRequestContext.Localization)
            };
        }

        private string GetPageUrlWithoutFormatParameter()
        {
            NameValueCollection filtered = HttpUtility.ParseQueryString(HttpContext.Current.Request.QueryString.ToString());
            filtered.Remove("format");
            return HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path) + (filtered.Count>0 ?  "?" + filtered : "");
        }
    }
}
