using System.Collections.Generic;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Navigation;

namespace Sdl.Web.Common.Interfaces
{
    /// <summary>
    /// Interface implemented by Navigation Providers which support "on-demand" navigation (i.e. can provide navigation subtrees).
    /// </summary>
    public interface IOnDemandNavigationProvider
    {
        /// <summary>
        /// Gets a Navigation subtree for the given Sitemap Item.
        /// </summary>
        /// <param name="sitemapItemId">The context <see cref="SitemapItem"/> identifier. Can be <c>null</c>.</param>
        /// <param name="filter">The <see cref="NavigationFilter"/> used to specify which information to put in the subtree.</param>
        /// <param name="localization">The context <see cref="ILocalization"/>.</param>
        /// <returns>A set of Sitemap Items representing the requested subtree.</returns>
        IEnumerable<SitemapItem> GetNavigationSubtree(string sitemapItemId, NavigationFilter filter, Localization localization);
    }
}
