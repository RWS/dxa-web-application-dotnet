using System;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.Configuration;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Mvc.Configuration;

namespace Sdl.Web.Mvc.Html
{
    /// <summary>
    /// Media helper to be used when using Contextual Image Delivery
    /// </summary>
    public class ContextualMediaHelper : BaseMediaHelper
    {
        private readonly string _cidBaseUrl;

        public ContextualMediaHelper()
        {
            ImageResizeUrlFormat = "{0}/scale/{1}x{2}/{3}{4}";
            // the exclusion of the app setting does mean the proxy will operate on all requests and we don't want that
            // so we must force the use of this setting by triggering an exception if its empty
            string pattern = WebConfigurationManager.AppSettings["cid-service-proxy-pattern"] ?? string.Empty;
            _cidBaseUrl = pattern.Replace("*", "").Replace("?", "").TrimEnd('/');
            if (string.IsNullOrEmpty(_cidBaseUrl))
            {
                throw new DxaException("cid-service-proxy-pattern cannot be empty when ContextualMediaHelper is enabled.");
            }          
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
            string height = (aspect == 0) ? String.Empty : ((int)Math.Ceiling(width / aspect)).ToString(CultureInfo.InvariantCulture);
            
            //Build the URL
            url = SiteConfiguration.MakeFullUrl(url, WebRequestContext.Localization);
            string prefix = url.StartsWith("https") ? "https/" : string.Empty;
            // should encode the url incase it contains special chars in a query string or something
            url = WebUtility.UrlEncode(url.Substring(url.IndexOf("://", StringComparison.Ordinal) + 3));
            return String.Format(ImageResizeUrlFormat, _cidBaseUrl, width, height, prefix, url);
        }
    }
}
