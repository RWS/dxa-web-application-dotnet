using System;
using System.Linq;
using System.Web.Mvc;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Mvc.Configuration;

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
            Localization loc = WebRequestContext.Localization;
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }
            string version = loc.Version;
            if (String.IsNullOrEmpty(localization) && WebRequestContext.Localization.SiteLocalizations != null && WebRequestContext.Localization.SiteLocalizations.Count > 0)
            {
                Localization defaultLoc = WebRequestContext.Localization.SiteLocalizations.FirstOrDefault(l => l.IsDefaultLocalization) ?? WebRequestContext.Localization;
                localization = defaultLoc.Path;
            }
            if (!String.IsNullOrEmpty(version))
            {
                version = "/" + version;
            }
            path = "~" + localization + "/system" + version + path;
            return helper.Content(path);
        }

        /// <summary>
        /// Generates a responsive image URL.
        /// </summary>
        /// <param name="urlHelper"></param>
        /// <param name="sourceImageUrl"></param>
        /// <param name="aspect"></param>
        /// <param name="widthFactor"></param>
        /// <param name="containerSize"></param>
        /// <returns>The responsive image URL.</returns>
        /// <remarks>This is a thin wrapper around <see cref="IMediaHelper.GetResponsiveImageUrl"/> intended to make view code simpler.</remarks>
        public static string ResponsiveImage(this UrlHelper urlHelper, string sourceImageUrl, double aspect, string widthFactor, int containerSize = 0)
        {
            return SiteConfiguration.MediaHelper.GetResponsiveImageUrl(sourceImageUrl, aspect, widthFactor, containerSize);
        }

    }
}
