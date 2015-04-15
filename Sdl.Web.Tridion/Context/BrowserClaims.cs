using System;
using System.Collections.Generic;
using System.Linq;

namespace Sdl.Web.Tridion.Context
{
    /// <summary>
    /// Based on https://github.com/NunoLinhares/TridionContextEngineWrapper/tree/master/Sdl.Tridion.Context
    /// </summary>
    public class BrowserClaims : ContextClaims
    {
        public BrowserClaims(Dictionary<Uri, object> claims) : base(claims)
        {
        }

        public BrowserClaims()
        {
        }

        public bool CookieSupport
        {
            get
            {
                // BUG: Returns false on desktop FF, IE, Safari
                return GetBooleanValue(ClaimUris.UriCookieSupport);
            }
        }

        public string CssVersion { get { return GetStringValue(ClaimUris.UriCssVersion); } }

        public int DisplayColorDepth { get { return GetIntValue(ClaimUris.UriDisplayColorDepth); } }

        public int DisplayHeight { get { return GetIntValue(ClaimUris.UriBrowserDisplayHeight); } }

        public int DisplayWidth 
        { 
            get 
            { 
                var val = GetIntValue(ClaimUris.UriBrowserDisplayWidth);
                //The context engine uses a default of 800, which we override to 1024
                if (val == 800 && this.IsUsingDefaults)
                {
                    val = 1024;
                }
                return val;
            }
        }

        public List<string> ImageFormatSupport
        {
            get
            {
                string x = GetStringValue(ClaimUris.UriImageFormatSupport);
                return x.Split(',').ToList();
            }
        }

        public List<string> InputDevices
        {
            get
            {
                string x = GetStringValue(ClaimUris.UriInputDevices);
                return x.Split(',').ToList();
            }
        }

        public string InputModeSupport
        {
            get { return GetStringValue(ClaimUris.UriInputModeSupport); }
        }

        public string JsVersion { get { return GetStringValue(ClaimUris.UriJsVersion); } }

        public string MarkupSupport
        {
            get { return GetStringValue(ClaimUris.UriMarkupSupport); }
        }
        public string Model
        {
            get { return GetStringValue(ClaimUris.UriBrowserModel); }
        }

        public string PreferredHtmlContentType { get { return GetStringValue(ClaimUris.UriPreferredHtmlContentType); } }

        public string ScriptSupport { get { return GetStringValue(ClaimUris.UriScriptSupport); } }

        public List<string> StylesheetSupport
        {
            get { return GetStringValue(ClaimUris.UriStylesheetSupport).Split(',').ToList(); }
        }

        public string Variant { get { return GetStringValue(ClaimUris.UriBrowserVariant); } }

        public string Vendor { get { return GetStringValue(ClaimUris.UriBrowserVendor); } }

        public string Version { get { return GetStringValue(ClaimUris.UriBrowserVersion); } }
    }
}
