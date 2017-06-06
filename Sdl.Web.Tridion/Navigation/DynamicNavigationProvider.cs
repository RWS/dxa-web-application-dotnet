using System.Collections.Generic;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Navigation;

namespace Sdl.Web.Tridion.Navigation
{
    /// <summary>
    /// Navigation Provider implementation based on Taxonomies (Categories and Keywords)
    /// </summary>
    public class DynamicNavigationProvider : INavigationProvider, IOnDemandNavigationProvider
    {
        private static readonly INavigationProvider DyanmicNavigationProvider = new Sdl.Web.Tridion.Navigation.ModelServiceImpl.DynamicNavigationProvider();
        
        #region INavigationProvider members
        /// <summary>
        /// Gets the Navigation Model (Sitemap) for a given Localization.
        /// </summary>
        /// <param name="localization">The Localization.</param>
        /// <returns>The Navigation Model (Sitemap root Item).</returns>
        public SitemapItem GetNavigationModel(Localization localization)
        {
            using (new Tracer(localization))
            {
                return DyanmicNavigationProvider.GetNavigationModel(localization);              
            }
        }

        /// <summary>
        /// Gets Navigation Links for the top navigation menu for the given request URL path.
        /// </summary>
        /// <param name="requestUrlPath">The request URL path.</param>
        /// <param name="localization">The Localization.</param>
        /// <returns>The Navigation Links.</returns>
        public NavigationLinks GetTopNavigationLinks(string requestUrlPath, Localization localization)
        {
            using (new Tracer(requestUrlPath, localization))
            {
                return DyanmicNavigationProvider.GetTopNavigationLinks(requestUrlPath, localization);
            }
        }

        /// <summary>
        /// Gets Navigation Links for the context navigation panel for the given request URL path.
        /// </summary>
        /// <param name="requestUrlPath">The request URL path.</param>
        /// <param name="localization">The Localization.</param>
        /// <returns>The Navigation Links.</returns>
        public NavigationLinks GetContextNavigationLinks(string requestUrlPath, Localization localization)
        {
            using (new Tracer(requestUrlPath, localization))
            {
                return DyanmicNavigationProvider.GetContextNavigationLinks(requestUrlPath, localization);
            }
        }

        /// <summary>
        /// Gets Navigation Links for the breadcrumb trail for the given request URL path.
        /// </summary>
        /// <param name="requestUrlPath">The request URL path.</param>
        /// <param name="localization">The Localization.</param>
        /// <returns>The Navigation Links.</returns>
        public NavigationLinks GetBreadcrumbNavigationLinks(string requestUrlPath, Localization localization)
        {
            using (new Tracer(requestUrlPath, localization))
            {
                return DyanmicNavigationProvider.GetBreadcrumbNavigationLinks(requestUrlPath, localization);
            }
        }
        #endregion

        #region IOnDemandNavigationProvider members
        /// <summary>
        /// Gets a Navigation subtree for the given Sitemap Item.
        /// </summary>
        /// <param name="sitemapItemId">The context <see cref="SitemapItem"/> identifier. Can be <c>null</c>.</param>
        /// <param name="filter">The <see cref="NavigationFilter"/> used to specify which information to put in the subtree.</param>
        /// <param name="localization">The context <see cref="Localization"/>.</param>
        /// <returns>A set of Sitemap Items representing the requested subtree.</returns>
        public IEnumerable<SitemapItem> GetNavigationSubtree(string sitemapItemId, NavigationFilter filter, Localization localization)
        {
            using (new Tracer(sitemapItemId, filter, localization))
            {
                return ((IOnDemandNavigationProvider)DyanmicNavigationProvider).GetNavigationSubtree(sitemapItemId, filter, localization);
            }
        }
        #endregion     
    }
}
