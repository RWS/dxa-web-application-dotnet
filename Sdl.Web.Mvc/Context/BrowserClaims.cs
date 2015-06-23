namespace Sdl.Web.Mvc.Context
{
    /// <summary>
    /// Represents the claims about the user's browser.
    /// </summary>
    /// <remarks>
    /// Based on https://github.com/NunoLinhares/TridionContextEngineWrapper/tree/master/Sdl.Tridion.Context
    /// </remarks>
    public class BrowserClaims : ContextClaims
    {
        /// <summary>
        /// Gets the name of the "aspect" which the strongly typed claims class represents.
        /// </summary>
        /// <returns>The name of the aspect.</returns>
        protected internal override string GetAspectName()
        {
            return "browser";
        }

        public bool CookieSupport
        {
            get
            {
                // BUG: Returns false on desktop FF, IE, Safari
                return GetClaimValue<bool>("cookieSupport");
            }
        }

        public string CssVersion
        {
            get { return GetClaimValue<string>("cssVersion"); }
        }

        public int DisplayColorDepth
        {
            get { return GetClaimValue<int>("displayColorDepth"); }
        }

        public int DisplayHeight
        {
            get { return GetClaimValue<int>("displayHeight"); }
        }

        public int DisplayWidth 
        { 
            get { return GetClaimValue<int>("displayWidth"); }
        }

        public string[] ImageFormatSupport
        {
            get
            {
                string imageFormatSupport = GetClaimValue<string>("imageFormatSupport");
                return imageFormatSupport == null ? null : imageFormatSupport.Split(',');
            }
        }

        public string[] InputDevices
        {
            get
            {
                string inputDevices = GetClaimValue<string>("inputDevices");
                return inputDevices == null ? null : inputDevices.Split(',');
            }
        }

        public string InputModeSupport
        {
            get { return GetClaimValue<string>("inputModeSupport"); }
        }

        public string JsVersion
        {
            get { return GetClaimValue<string>("jsVersion"); }
        }

        public string MarkupSupport
        {
            get { return GetClaimValue<string>("markupSupport"); }
        }

        public string Model
        {
            get { return GetClaimValue<string>("model"); }
        }

        public string PreferredHtmlContentType
        {
            get { return GetClaimValue<string>("preferredHtmlContentType"); }
        }

        public string ScriptSupport
        {
            get { return GetClaimValue<string>("scriptSupport"); }
        }

        public string[] StylesheetSupport
        {
            get
            {
                string stylesheetSupport = GetClaimValue<string>("stylesheetSupport");
                return stylesheetSupport == null ? null : stylesheetSupport.Split(',');
            }
        }

        public string Variant
        {
            get { return GetClaimValue<string>("variant"); }
        }

        public string Vendor
        {
            get { return GetClaimValue<string>("vendor"); }
        }

        public string Version
        {
            get { return GetClaimValue<string>("version"); }
        }
    }
}
