using Sdl.Web.Common.Configuration;
using Sdl.Web.Mvc.Configuration;
using System;
using System.Globalization;

namespace Sdl.Web.Mvc.Html
{
    /// <summary>
    /// Media helper to be used when using Contextual Image Delivery
    /// </summary>
    public class ContextualMediaHelper : BaseMediaHelper
    {
        public string ImageResizeRoute { get; set; }
        private const string SingleSiteToken = "source/site";
        public ContextualMediaHelper()
        {
            ImageResizeUrlFormat = "/{0}/scale/{1}x{2}/{3}{4}";
            ImageResizeRoute = "cid";
        }

        public override string GetResponsiveImageUrl(string url, double aspect, string widthFactor, int containerSize = 0)
        {
            int width = GetResponsiveWidth(widthFactor, containerSize);
            //Round the width to the nearest set limit point - important as we do not want 
            //to swamp the cache with lots of different sized versions of the same image
            for (int i = 0; i < ImageWidths.Count; i++)
            {
                if (width <= ImageWidths[i] || i == ImageWidths.Count - 1)
                {
                    width = ImageWidths[i];
                    break;
                }
            }
            //Height is calculated from the aspect ratio (0 means preserve aspect ratio)
            string height = aspect == 0 ? String.Empty : ((int)Math.Ceiling(width / aspect)).ToString(CultureInfo.InvariantCulture);
            //Build the URL
            url = SiteConfiguration.MakeFullUrl(url, WebRequestContext.Localization);
            string prefix = SingleSiteToken;
            if (url.StartsWith("http"))
            {
                prefix = "";
                if (url.StartsWith("https"))
                {
                    prefix = "https/";
                }
                url = url.Substring(url.IndexOf("://", StringComparison.Ordinal) + 3);
            }
            return String.Format(ImageResizeUrlFormat, ImageResizeRoute, width, height, prefix, url);
        }
    }
}
