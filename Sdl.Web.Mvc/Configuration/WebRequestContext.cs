using Sdl.Web.Common.Configuration;
using Sdl.Web.Mvc.Context;
using System;
using System.Web;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Logging;
using System.Collections.Generic;
using Sdl.Web.Delivery.ServicesCore.ClaimStore;

namespace Sdl.Web.Mvc.Configuration
{
    /// <summary>
    /// Container for request level context data, wraps the HttpContext.Items dictionary, which is used for this purpose
    /// </summary>
    public class WebRequestContext
    {
        private const int MaxWidth = 1024;
        private const string PreviewSessionTokenHeader = "x-preview-session-token";
        private const string PreviewSessionTokenCookie = "preview-session-token";

        /// <summary>
        /// The current request localization
        /// </summary>
        public static Localization Localization
        {
            get
            {
                return (Localization)GetFromContextStore("Localization") ?? (Localization)AddToContextStore("Localization", GetCurrentLocalization());
            }
            set
            {
                AddToContextStore("Localization", value);
            }
        }

        /// <summary>
        /// The Tridion Context Engine
        /// </summary>
        public static ContextEngine ContextEngine
        {
            get
            {
                ContextEngine result = (ContextEngine)GetFromContextStore("ContextEngine");
                if (result == null)
                {
                    try
                    {
                        result = new ContextEngine();
                        AddToContextStore("ContextEngine", result);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                        throw;
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// The maximum width for media objects for this requests display width
        /// </summary>
        public static int MaxMediaWidth
        {
            get
            {
                //Pixel Ratio can be non-integer value (if zoom is applied to browser) - so we use a min of 1, and otherwise round when calculating max width
                double pixelRatio = ContextEngine.GetClaims<DeviceClaims>().PixelRatio;
                int displayWidth = IsContextCookiePresent ? ContextEngine.GetClaims<BrowserClaims>().DisplayWidth : 1024;
                if (displayWidth == 0) displayWidth = MaxWidth;
                return (int?)GetFromContextStore("MaxMediaWidth") ?? (int)AddToContextStore("MaxMediaWidth", Convert.ToInt32(Math.Max(1.0, pixelRatio) * Math.Min(displayWidth, MaxWidth)));
            }
        }

        /// <summary>
        /// The size of display of the device which initiated this request
        /// </summary>
        public static ScreenWidth ScreenWidth
        {
            get
            {
                object val = GetFromContextStore("ScreenWidth");
                return (ScreenWidth?) val ?? (ScreenWidth)AddToContextStore("ScreenWidth", CalculateScreenWidth());
            }
        }

        /// <summary>
        /// The current request URL
        /// </summary>
        public static string RequestUrl => HttpContext.Current.Request.Url.ToString();

        /// <summary>
        /// String array of client-supported MIME accept types
        /// </summary>
        public static string[] AcceptTypes => HttpContext.Current.Request.AcceptTypes;

        /// <summary>
        /// Current Page Model
        /// </summary>
        public static PageModel PageModel
        {
            get
            {
                return (PageModel) GetFromContextStore("PageModel");
            }
            set
            {
                AddToContextStore("PageModel", value);
            }
        }

        /// <summary>
        /// True if the request is for localhost domain
        /// </summary>
        public static bool IsDeveloperMode => (bool?)GetFromContextStore("IsDeveloperMode") ?? (bool)AddToContextStore("IsDeveloperMode", GetIsDeveloperMode());

        private static bool GetIsDeveloperMode()
        {
            try
            {
                if (HttpContext.Current != null)
                {
                    return HttpContext.Current.Request.Url.Host.ToLower() == "localhost";
                }
            }
            catch (Exception)
            {
                //Do nothing
            }
            return false;
        }

        /// <summary>
        /// True if the request is from XPM (NOTE currently always true for staging as we cannot reliably distinguish XPM requests)
        /// </summary>
        [Obsolete("Use WebRequestContext.IsSessionPreview or Localization.IsXpmEnabled")]
        public static bool IsPreview 
            => (bool?)GetFromContextStore("IsPreview") ?? (bool)AddToContextStore("IsPreview", Localization.IsXpmEnabled);

        /// <summary>
        /// True if the request is from XPM Session Preview
        /// </summary>
        public static bool IsSessionPreview
        {
            get
            {
                var claimStore = AmbientDataContext.CurrentClaimStore;
                if (claimStore == null) return false;

                var headers = claimStore?.Get<Dictionary<string, string[]>>(new Uri(WebClaims.REQUEST_HEADERS));
                if (headers != null && headers.ContainsKey(PreviewSessionTokenHeader))
                {
                    return true;
                }

                var cookies = claimStore?.Get<Dictionary<string, string>>(new Uri(WebClaims.REQUEST_COOKIES));
                if (cookies != null && cookies.ContainsKey(PreviewSessionTokenCookie))
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// True if the request is an include page
        /// </summary>
        public static bool IsInclude 
            => (bool?)GetFromContextStore("IsInclude") ?? (bool)AddToContextStore("IsInclude", RequestUrl.Contains("system/include/"));

        /// <summary>
        /// Cache key salt used to "mix" in with keys used for caching to provie uniqueness per request.
        /// </summary>
        public static long CacheKeySalt
        {
            get
            {
                return (long?)GetFromContextStore("CacheKeySalt") ?? 0;
            }
            set
            {
                AddToContextStore("CacheKeySalt", value);
            }
        }      
    
        protected static ScreenWidth CalculateScreenWidth()
        {
            int width = IsContextCookiePresent ? ContextEngine.GetClaims<BrowserClaims>().DisplayWidth : MaxWidth;
            // zero width is not valid and probably means the context engine was not correctly initialized so
            // again default to 1024
            if (width == 0) width = MaxWidth;
            if (width < SiteConfiguration.MediaHelper.SmallScreenBreakpoint)
            {
                return ScreenWidth.ExtraSmall;
            }
            if (width < SiteConfiguration.MediaHelper.MediumScreenBreakpoint)
            {
                return ScreenWidth.Small;
            }
            if (width < SiteConfiguration.MediaHelper.LargeScreenBreakpoint)
            {
                return ScreenWidth.Medium;
            }
            return ScreenWidth.Large;
        }

        protected static Localization GetCurrentLocalization() 
            => HttpContext.Current == null ? null : SiteConfiguration.LocalizationResolver.ResolveLocalization(HttpContext.Current.Request.Url);

        protected static object GetFromContextStore(string key) 
            => HttpContext.Current == null ? null : HttpContext.Current.Items[key];

        protected static object AddToContextStore(string key, object value)
        {
            if (HttpContext.Current == null) return value;
            HttpContext.Current.Items[key] = value;
            return value;
        }

        private static bool IsContextCookiePresent
        {
            get
            {
                HttpContext httpContext = HttpContext.Current;
                return httpContext?.Request.Cookies["context"] != null;
            }
        }
    }
}
