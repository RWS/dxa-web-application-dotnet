using System;

namespace Sdl.Web.Tridion.Context
{
    /// <summary>
    /// Based on https://github.com/NunoLinhares/TridionContextEngineWrapper/tree/master/Sdl.Tridion.Context
    /// </summary>
    public static class ClaimUris
    {
        private const string UriContext = "taf:claim:context:";
        private const string UriDevice = UriContext + "device:";
        private const string UriBrowser = UriContext + "browser:";
        private const string UriOs = UriContext + "os:";

        public static Uri UriCookieSupport = new Uri(UriBrowser + "cookieSupport");
        public static Uri UriCssVersion = new Uri(UriBrowser + "cssVersion");
        public static Uri UriDisplayColorDepth = new Uri(UriBrowser + "displayColorDepth");
        public static Uri UriBrowserDisplayHeight = new Uri(UriBrowser + "displayHeight");
        public static Uri UriBrowserDisplayWidth = new Uri(UriBrowser + "displayWidth");
        public static Uri UriImageFormatSupport = new Uri(UriBrowser + "imageFormatSupport");
        public static Uri UriInputDevices = new Uri(UriBrowser + "inputDevices");
        public static Uri UriInputModeSupport = new Uri(UriBrowser + "inputModeSupport");
        public static Uri UriJsVersion = new Uri(UriBrowser + "jsVersion");
        public static Uri UriMarkupSupport = new Uri(UriBrowser + "markupSupport");
        public static Uri UriBrowserModel = new Uri(UriBrowser + "model");
        public static Uri UriPreferredHtmlContentType = new Uri(UriBrowser + "preferredHtmlContentType");
        public static Uri UriScriptSupport = new Uri(UriBrowser + "scriptSupport");
        public static Uri UriStylesheetSupport = new Uri(UriBrowser + "stylesheetSupport");
        public static Uri UriBrowserVariant = new Uri(UriBrowser + "variant");
        public static Uri UriBrowserVendor = new Uri(UriBrowser + "vendor");
        public static Uri UriBrowserVersion = new Uri(UriBrowser + "version");

        public static Uri UriDeviceDisplayHeight = new Uri(UriDevice + "displayHeight");
        public static Uri UriDeviceDisplayWidth = new Uri(UriDevice + "displayWidth");
        public static Uri UriMobile = new Uri(UriDevice + "mobile");
        public static Uri UriDeviceModel = new Uri(UriDevice + "model");
        public static Uri UriPixelDensity = new Uri(UriDevice + "pixelDensity");
        public static Uri UriPixelRatio = new Uri(UriDevice + "pixelRatio");
        public static Uri UriRobot = new Uri(UriDevice + "robot");
        public static Uri UriTablet = new Uri(UriDevice + "tablet");
        public static Uri UriDeviceVariant = new Uri(UriDevice + "variant");
        public static Uri UriDeviceVendor = new Uri(UriDevice + "vendor");
        public static Uri UriDeviceVersion = new Uri(UriDevice + "version");

        public static Uri UriOsModel = new Uri(UriOs + "model");
        public static Uri UriOsVariant = new Uri(UriOs + "variant");
        public static Uri UriOsVendor = new Uri(UriOs + "vendor");
        public static Uri UriOsVersion = new Uri(UriOs + "version");
    }
}
