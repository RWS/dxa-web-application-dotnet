using Sdl.Web.Mvc.Configuration;
using System;
using System.Web.Mvc;

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
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }
            string version = WebRequestContext.Localization.Version;
            if (!String.IsNullOrEmpty(version))
            {
                version = "/" + version;
            }
            path = "~/" + localization + "system" + version + path;
            return helper.Content(path);
        }
    }
}
