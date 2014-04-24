using Sdl.Web.Tridion.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Sdl.Web.Mvc
{
    /// <summary>
    /// Container for request level context data, wraps the HttpContext.Items dictionary, which is used for this purpose
    /// </summary>
    public class WebRequestContext
    {
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
                return (int?)GetFromContextStore("MaxMediaWidth") ?? (int)AddToContextStore("MaxMediaWidth", ContextEngine.Device.PixelRatio * ContextEngine.Browser.DisplayWidth);
            }
        }

        protected static Localization GetCurrentLocalization()
        {
            //If theres a single localization use that regardless
            if (Configuration.Localizations.Count == 1)
            {
                return Configuration.Localizations.SingleOrDefault().Value;
            }
            if (HttpContext.Current != null && HttpContext.Current.Request != null)
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
            //TODO - should we throw an error instead?
            return new Localization { LocalizationId = 0, Culture = "en-US", Path = "" };
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
