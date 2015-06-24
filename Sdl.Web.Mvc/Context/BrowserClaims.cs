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
            get { return GetClaimValues<string>("imageFormatSupport"); }
        }

        public string[] InputDevices
        {
            get { return GetClaimValues<string>("inputDevices"); }
        }

        public string[] InputModeSupport
        {
            get { return GetClaimValues<string>("inputModeSupport"); }
        }

        public string JsVersion
        {
            get { return GetClaimValue<string>("jsVersion"); }
        }

        public string[] MarkupSupport
        {
            get { return GetClaimValues<string>("markupSupport"); }
        }

        public string Model
        {
            get { return GetClaimValue<string>("model"); }
        }

        public string PreferredHtmlContentType
        {
            get { return GetClaimValue<string>("preferredHtmlContentType"); }
        }

        public string[] ScriptSupport
        {
            get { return GetClaimValues<string>("scriptSupport"); }
        }

        public string[] StylesheetSupport
        {
            get { return GetClaimValues<string>("stylesheetSupport"); }
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
