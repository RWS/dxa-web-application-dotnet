using System.Web.Mvc;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Mvc.Configuration;

namespace Sdl.Web.Mvc.Html
{
    /// <summary>
    /// Extension methods for the UrlHelper
    /// </summary>
    public static class UrlHelperExtensions
    {
        /// <summary>
        /// Gets a versioned URL (including the version number of the HTML design/assets).
        /// </summary>
        /// <param name="helper">The <see cref="UrlHelper"/> instance on which this extension method operates.</param>
        /// <param name="relativePath">The (unversioned) URL path relative to the system folder.</param>
        /// <param name="localization">Not used (deprecated).</param>
        /// <returns>A versioned URL path (server-relative).</returns>
        /// <remarks>
        /// Versioned URLs are used to facilitate agressive caching of those assets; see <see cref="Sdl.Web.Mvc.Statics.StaticContentModule"/>.
        /// </remarks>
        public static string VersionedContent(this UrlHelper helper, string relativePath, string localization = "")
        {
            string versionedUrlPath = WebRequestContext.Localization.GetVersionedUrlPath(relativePath);
            return helper.Content(versionedUrlPath);
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
        /// <remarks>This is a thin wrapper around <see cref="Sdl.Web.Common.Interfaces.IMediaHelper.GetResponsiveImageUrl"/> intended to make view code simpler.</remarks>
        public static string ResponsiveImage(this UrlHelper urlHelper, string sourceImageUrl, double aspect, string widthFactor, int containerSize = 0)
        {
            return SiteConfiguration.MediaHelper.GetResponsiveImageUrl(sourceImageUrl, aspect, widthFactor, containerSize);
        }

        /// <summary>
        /// Normalizes a URL path for a Page.
        /// </summary>
        /// <remarks>
        /// The following normalization actions are taken:
        /// <list type="bullet">
        ///     <item>Ensure the URL path is extensionless.</item>
        ///     <item>Ensure the URL path for an index page ends with "/index".</item>
        /// </list>
        /// </remarks>
        /// <param name="urlPath">The input URL path.</param>
        /// <returns>The normalized URL path.</returns>
        public static string NormalizePageUrlPath(this UrlHelper urlHelper, string urlPath)
        {
            return urlPath.NormalizePageUrlPath();
        }
    }
}
