using Sdl.Web.Mvc.Configuration;
using System;
using System.Web.Mvc;
using System.Linq;

namespace Sdl.Web.Mvc.Html
{
    /// <summary>
    /// Extension methods for the UrlHelper
    /// </summary>
    public static class UrlHelperExtensions
    {
        /// <summary>
        /// Add a version number to a static asset URL
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="path">The URL to add the version to</param>
        /// <param name="localization">The localization to add to the URL</param>
        /// <returns>A localized, versioned URL</returns>
        public static string VersionedContent(this UrlHelper helper, string path, string localization = "")
        {
            var loc = WebRequestContext.Localization;
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }
            string version = loc.Version;
            if (localization == "" && WebRequestContext.Localization.SiteLocalizations!=null && WebRequestContext.Localization.SiteLocalizations.Count>0)
            {
                var defaultLoc = WebRequestContext.Localization.SiteLocalizations.Where(l=>l.IsDefaultLocalization).FirstOrDefault() ?? WebRequestContext.Localization;
                localization = defaultLoc.Path;
            }
            if (!String.IsNullOrEmpty(version))
            {
                version = "/" + version;
            }
            path = "~" + localization + "/system" + version + path;
            return helper.Content(path);
        }
    }
}
