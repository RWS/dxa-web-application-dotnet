using System.Collections.Generic;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Navigation;
using Sdl.Web.Delivery.Service;

namespace Sdl.Web.Tridion.TridionDocs.Navigation
{
    public class TocProvider
    {
        protected IOnDemandNavigationProvider NavigationProvider => new DocsNavigationProvider();

        public IEnumerable<SitemapItem> GetToc(ILocalization localization)
           => GetToc(localization, null, false, 1);

        public IEnumerable<SitemapItem> GetToc(ILocalization localization, string sitemapItemId)
          => GetToc(localization, sitemapItemId, false, 1);

        public IEnumerable<SitemapItem> GetToc(ILocalization localization, string sitemapItemId, bool includeAncestors)
            => GetToc(localization, sitemapItemId, includeAncestors, 1);

        public IEnumerable<SitemapItem> GetToc(ILocalization localization, string sitemapItemId, bool includeAncestors,
            int descendantLevels)
        {
            bool caching = ServiceCacheProvider.Instance.DisableCaching;
            ServiceCacheProvider.Instance.DisableCaching = true;
            IOnDemandNavigationProvider onDemandNavigationProvider = NavigationProvider;
            NavigationFilter navigationFilter = new NavigationFilter
            {
                DescendantLevels = descendantLevels,
                IncludeAncestors = includeAncestors
            };

            var result = onDemandNavigationProvider.GetNavigationSubtree(sitemapItemId, navigationFilter, localization);
            ServiceCacheProvider.Instance.DisableCaching = caching;
            return result;
        }
    }
}
