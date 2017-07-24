using System.Collections.Generic;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Navigation;
using Sdl.Web.Tridion.R2Mapping;

namespace Sdl.Web.Tridion.Navigation
{
    /// <summary>
    /// Navigation Provider implementation based on Taxonomies (Categories and Keywords)
    /// </summary>
    public class DynamicNavigationProvider : INavigationProvider, IOnDemandNavigationProvider
    {
        private static INavigationProvider _dyanmicNavigationProviderR2; 
        private static INavigationProvider _dyanmicNavigationProviderLegacy;

        private INavigationProvider NavigationProvider
        {
            get
            {
                if (SiteConfiguration.ContentProvider is DefaultContentProviderR2)
                {
                    return _dyanmicNavigationProviderR2 ??
                           (_dyanmicNavigationProviderR2 = new ModelServiceImpl.DynamicNavigationProvider());
                }
                return _dyanmicNavigationProviderLegacy ??
                       (_dyanmicNavigationProviderLegacy = new CILImpl.DynamicNavigationProvider());
            }
        }

        #region INavigationProvider members
        /// <summary>
        /// Gets the Navigation Model (Sitemap) for a given Localization.
        /// </summary>
        /// <param name="localization">The Localization.</param>
        /// <returns>The Navigation Model (Sitemap root Item).</returns>
        public SitemapItem GetNavigationModel(Localization localization) 
            => NavigationProvider.GetNavigationModel(localization);

        /// <summary>
        /// Gets Navigation Links for the top navigation menu for the given request URL path.
        /// </summary>
        /// <param name="requestUrlPath">The request URL path.</param>
        /// <param name="localization">The Localization.</param>
        /// <returns>The Navigation Links.</returns>
        public NavigationLinks GetTopNavigationLinks(string requestUrlPath, Localization localization) 
            => NavigationProvider.GetTopNavigationLinks(requestUrlPath, localization);

        /// <summary>
        /// Gets Navigation Links for the context navigation panel for the given request URL path.
        /// </summary>
        /// <param name="requestUrlPath">The request URL path.</param>
        /// <param name="localization">The Localization.</param>
        /// <returns>The Navigation Links.</returns>
        public NavigationLinks GetContextNavigationLinks(string requestUrlPath, Localization localization) 
            => NavigationProvider.GetContextNavigationLinks(requestUrlPath, localization);

        /// <summary>
        /// Gets Navigation Links for the breadcrumb trail for the given request URL path.
        /// </summary>
        /// <param name="requestUrlPath">The request URL path.</param>
        /// <param name="localization">The Localization.</param>
        /// <returns>The Navigation Links.</returns>
        public NavigationLinks GetBreadcrumbNavigationLinks(string requestUrlPath, Localization localization) 
            => NavigationProvider.GetBreadcrumbNavigationLinks(requestUrlPath, localization);

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
            => ((IOnDemandNavigationProvider)NavigationProvider).GetNavigationSubtree(sitemapItemId, filter, localization);

        #endregion     
    }
}
