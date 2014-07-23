using System;
using System.Linq;
using System.Web;
using Sdl.Web.Common;
using Sdl.Web.Tridion.Context;

namespace Sdl.Web.Mvc
{
    /// <summary>
    /// Container for request level context data, wraps the HttpContext.Items dictionary, which is used for this purpose
    /// </summary>
    public class WebRequestContext
    {
        private const int maxWidth = 1024;

        public static Localization Localization
        {
            get
            {
                return (Localization)GetFromContextStore("Localization") ?? (Localization)AddToContextStore("Localization", GetCurrentLocalization());
            }       
        }

        public static ContextEngine ContextEngine
        {
            get
            {
                return (ContextEngine)GetFromContextStore("ContextEngine") ?? (ContextEngine)AddToContextStore("ContextEngine", new ContextEngine());
            }
        }
        
        public static int MaxMediaWidth
        {
            get
            {
                //Pixel Ratio can be non-integer value (if zoom is applied to browser) - so we use a min of 1, and otherwise round when calculating max width
                return (int?)GetFromContextStore("MaxMediaWidth") ?? (int)AddToContextStore("MaxMediaWidth", Math.Max(1, Math.Min(1, Convert.ToInt32(ContextEngine.Device.PixelRatio))) * Math.Min(ContextEngine.Browser.DisplayWidth, maxWidth));
            }
        }

        public static ScreenWidth ScreenWidth
        {
            get
            {
                var val = GetFromContextStore("ScreenWidth");
                return val == null ? (ScreenWidth)AddToContextStore("ScreenWidth", CalculateScreenWidth()) : (ScreenWidth)val;
            }
        }

        public static string GetRequestUrl()
        {
            return HttpContext.Current.Request.Url.ToString();
        }

        public static string PageId
        {
            get
            {
                return (string)GetFromContextStore("PageId");
            }
            set
            {
                AddToContextStore("PageId", value);
            }
        }

        protected static ScreenWidth CalculateScreenWidth()
        {
            int width = ContextEngine.Browser.DisplayWidth;
            if (width < Configuration.MediaHelper.SmallScreenBreakpoint)
            {
                return ScreenWidth.ExtraSmall;
            }
            if (width < Configuration.MediaHelper.MediumScreenBreakpoint)
            {
                return ScreenWidth.Small;
            }
            if (width < Configuration.MediaHelper.LargeScreenBreakpoint)
            {
                return ScreenWidth.Medium;
            }
            return ScreenWidth.Large;
        }

        public static bool IsDeveloperMode
        {
            get
            {
                return (bool?)GetFromContextStore("IsDeveloperMode") ?? (bool)AddToContextStore("IsDeveloperMode", Localization.Domain=="localhost");
            }
        }

        public static bool IsPreview
        {
            //For now we cannot reliably detect when we are in experience manager, so we set this to be true whenever we are in staging
            get
            {
                return (bool?)GetFromContextStore("IsPreview") ?? (bool)AddToContextStore("IsPreview", Configuration.IsStaging);
            }
        }

        protected static Localization GetCurrentLocalization()
        {
            //If theres a single localization use that regardless
            if (Configuration.Localizations.Count == 1)
            {
                return Configuration.Localizations.SingleOrDefault().Value;
            }
            try
            {
                if (HttpContext.Current != null)
                {
                    var uri = HttpContext.Current.Request.Url.AbsoluteUri;
                    foreach (var key in Configuration.Localizations.Keys)
                    {
                        if (uri.StartsWith(key))
                        {
                            Log.Debug("Request for {0} is from localization '{1}'", uri, Configuration.Localizations[key].Path);
                            return Configuration.Localizations[key];
                        }
                    }
                }
            }
            catch (Exception)
            {
                //Do nothing - In some cases we do not have a request (loading config on app start etc.) - we fallback on a default localization
            }
            return new Localization { LocalizationId = "0", Culture = "en-US", Path = "" };
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
    }
}
