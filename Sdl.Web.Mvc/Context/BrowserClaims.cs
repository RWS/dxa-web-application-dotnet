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
        protected internal override string GetAspectName() => "browser";

        public bool CookieSupport => GetClaimValue<bool>("cookieSupport");

        public string CssVersion => GetClaimValue<string>("cssVersion");

        public int DisplayColorDepth => GetClaimValue<int>("displayColorDepth");

        public int DisplayHeight => GetClaimValue<int>("displayHeight");

        public int DisplayWidth => GetClaimValue<int>("displayWidth");

        public string[] ImageFormatSupport => GetClaimValues<string>("imageFormatSupport");

        public string[] InputDevices => GetClaimValues<string>("inputDevices");

        public string[] InputModeSupport => GetClaimValues<string>("inputModeSupport");

        public string JsVersion => GetClaimValue<string>("jsVersion");

        public string[] MarkupSupport => GetClaimValues<string>("markupSupport");

        public string Model => GetClaimValue<string>("model");

        public string PreferredHtmlContentType => GetClaimValue<string>("preferredHtmlContentType");

        public string[] ScriptSupport => GetClaimValues<string>("scriptSupport");

        public string[] StylesheetSupport => GetClaimValues<string>("stylesheetSupport");

        public string Variant => GetClaimValue<string>("variant");

        public string Vendor => GetClaimValue<string>("vendor");

        public string Version => GetClaimValue<string>("version");
    }
}
