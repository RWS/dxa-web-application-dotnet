using System;
using System.Web.Mvc;

namespace Sdl.Web.Mvc.Html
{
    public static class UrlHelperExtensions
    {
        public static string VersionedContent(this UrlHelper helper, string path, string localization = "")
        {
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }
            string version = Common.Configuration.SiteConfiguration.SiteVersion;
            if (!String.IsNullOrEmpty(version))
            {
                version = "/" + version;
            }
            path = "~/" + localization + "system" + version + path;
            return helper.Content(path);
        }
    }
}
