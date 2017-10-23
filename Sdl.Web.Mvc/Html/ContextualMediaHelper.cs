using System;
using System.Globalization;
using System.Net;
using System.Web;
using System.Web.Configuration;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Mvc.Configuration;
using System.IO;

namespace Sdl.Web.Mvc.Html
{
    /// <summary>
    /// Media helper to be used when using Contextual Image Delivery
    /// </summary>
    public class ContextualMediaHelper : BaseMediaHelper
    {
        private readonly string _cidBaseUrl;
        private readonly string _hostname;

        public ContextualMediaHelper()
        {
            ImageResizeUrlFormat = "/{0}/scale/{1}x{2}/{3}{4}";               
            _cidBaseUrl = GetCidPath;
            if (string.IsNullOrEmpty(_cidBaseUrl))
            {
                throw new DxaException("cid-service-proxy-pattern cannot be empty when ContextualMediaHelper is enabled.");
            }

            // try to see if a user has added a mapping from localhost to some hostname
            _hostname = WebConfigurationManager.AppSettings["cid-localhost"] ?? string.Empty;
            if (string.IsNullOrEmpty(_hostname))
            {
                // fallback: attempt to auto resolve and this will work for the majority of time so
                // we can eliminate the need for users to add the setting
                _hostname = Dns.GetHostEntry("LocalHost").HostName;
            }
        }

        /// <summary>
        /// Returns the CID path that the proxy operates on (e.g. cid)
        /// </summary>
        public static string GetCidPath
        {
            get
            {
                // the exclusion of the app setting does mean the proxy will operate on all requests and we don't want 
                // that to happen so we must force the use of this setting by triggering an exception if its empty.
                string pattern = WebConfigurationManager.AppSettings["cid-service-proxy-pattern"] ?? string.Empty;
                return pattern.Replace("*", "").Replace("?", "").Trim('/');
            }
        }
       
        public override string GetResponsiveImageUrl(string url, double aspect, string widthFactor, int containerSize = 0)
        {
            string extension = Path.GetExtension(url);
            if (!IsSupported(extension)) return url;
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
            string height = (aspect == 0) ? string.Empty : ((int)Math.Ceiling(width / aspect)).ToString(CultureInfo.InvariantCulture);
            
            //Build the URL
            url = SiteConfiguration.MakeFullUrl(url, WebRequestContext.Localization);
            //remap localhost to real hostname for CID service
            Uri tmp = new Uri(url);           
            url = tmp.GetLeftPart(UriPartial.Authority).Replace("localhost", _hostname, StringComparison.InvariantCultureIgnoreCase) + tmp.PathAndQuery;
            // get prefix
            string prefix = url.StartsWith("https") ? "https/" : string.Empty;
            // should encode the url incase it contains special chars in a query string or something
            url = HttpUtility.UrlPathEncode(url.Substring(url.IndexOf("://", StringComparison.Ordinal) + 3));
            return string.Format(ImageResizeUrlFormat, _cidBaseUrl, width, height, prefix, url);
        }
    }
}
