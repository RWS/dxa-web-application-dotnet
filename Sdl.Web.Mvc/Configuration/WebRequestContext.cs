using System.Net;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Mvc.Context;
using System;
using System.Web;
using Sdl.Web.Common;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Mvc.Configuration
{
    /// <summary>
    /// Container for request level context data, wraps the HttpContext.Items dictionary, which is used for this purpose
    /// </summary>
    public class WebRequestContext
    {
        private const int _maxWidth = 1024;

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
        /// True if the current request is for a resource outside the scope of a particular localization
        /// </summary>
        [Obsolete("Deprecated in DXA 1.3. All requests will be in scope of a particular Localization; if no Localization can be determined, an exception will occur.")]
        public static bool HasNoLocalization
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotSupportedException("Setting this property is not supported in DXA 1.3.");
            }
        }

        /// <summary>
        /// The Tridion Context Engine
        /// </summary>
        public static ContextEngine ContextEngine
        {
            get
            {
                return (ContextEngine)GetFromContextStore("ContextEngine") ?? (ContextEngine)AddToContextStore("ContextEngine", new ContextEngine());
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
                int displayWidth = IsContextCookiePresent ? ContextEngine.GetClaims<BrowserClaims>().DisplayWidth : 0;
                if (displayWidth == 0)
                {
                    displayWidth = 1024;
                }
                return (int?)GetFromContextStore("MaxMediaWidth") ?? (int)AddToContextStore("MaxMediaWidth", Convert.ToInt32(Math.Max(1.0, pixelRatio) * Math.Min(displayWidth, _maxWidth)));
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
                return val == null ? (ScreenWidth)AddToContextStore("ScreenWidth", CalculateScreenWidth()) : (ScreenWidth)val;
            }
        }

        /// <summary>
        /// The current request URL
        /// </summary>
        public static string RequestUrl
        {
            get
            {
                return HttpContext.Current.Request.Url.ToString();
            }
        }

        /// <summary>
        /// String array of client-supported MIME accept types
        /// </summary>
        public static string[] AcceptTypes
        {
            get 
            { 
                return HttpContext.Current.Request.AcceptTypes;
            }
        }

        /// <summary>
        /// Identifier for the current page
        /// </summary>
        [Obsolete("Deprecated in DXA 1.6. Use WebRequestContext.PageModel instead.")]
        public static string PageId
        {
            get
            {
                return (PageModel == null) ? null : PageModel.Id;
            }
            set
            {
                throw new DxaException("Setting this property is not supported in DXA 1.6");
            }
        }

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
        public static bool IsDeveloperMode
        {
            get
            {
                return (bool?)GetFromContextStore("IsDeveloperMode") ?? (bool)AddToContextStore("IsDeveloperMode", GetIsDeveloperMode());
            }
        }

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
        public static bool IsPreview
        {
            //For now we cannot reliably detect when we are in experience manager, so we set this to be true whenever we are in staging
            get
            {
                return (bool?)GetFromContextStore("IsPreview") ?? (bool)AddToContextStore("IsPreview", Localization.IsStaging);
            }
        }

        /// <summary>
        /// True if the request is an include page
        /// </summary>
        public static bool IsInclude
        {
            // if request url contains "system/include" the include page is requested directly
            get
            {
                return (bool?)GetFromContextStore("IsInclude") ?? (bool)AddToContextStore("IsInclude", RequestUrl.Contains("system/include/"));
            }
        }

        protected static ScreenWidth CalculateScreenWidth()
        {
            int width = IsContextCookiePresent ? ContextEngine.GetClaims<BrowserClaims>().DisplayWidth : 0;
            if (width == 0)
            {
                width = 1024;
            }

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
        {
            if (HttpContext.Current == null)
            {
                return null;
            }
            return SiteConfiguration.LocalizationResolver.ResolveLocalization(HttpContext.Current.Request.Url);
        }
        
        protected static object GetFromContextStore(string key)
        {
            return HttpContext.Current == null ? null : HttpContext.Current.Items[key];
        }

        protected static object AddToContextStore(string key, object value)
        {
            if (HttpContext.Current != null)
            {
                HttpContext.Current.Items[key] = value;
            }
            return value;
        }

        private static bool IsContextCookiePresent
        {
            get
            {
                HttpContext httpContext = HttpContext.Current;
                return (httpContext != null) && (httpContext.Request.Cookies["context"] != null);
            }
        }

    }
}
