using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Tridion.Mapping;

namespace Sdl.Web.Tridion.Navigation
{
    /// <summary>
    /// Navigation Provider implementation based on statically generated (published) Navigation.json
    /// </summary>
    public class StaticNavigationProvider : INavigationProvider
    {
        private const string NavigationCacheRegionName = "Navigation_Static";

        #region INavigationProvider Members

        /// <summary>
        /// Gets the Navigation Model (Sitemap) for a given Localization.
        /// </summary>
        /// <param name="localization">The Localization.</param>
        /// <returns>The Navigation Model (Sitemap root Item).</returns>
        public virtual SitemapItem GetNavigationModel(Localization localization)
        {
            using (new Tracer(localization))
            {
                return SiteConfiguration.CacheProvider.GetOrAdd(
                    localization.LocalizationId,
                    NavigationCacheRegionName,
                    () => BuildNavigationModel(localization)
                    // TODO: dependency on navigation.json Page
                    );
            }
        }

        /// <summary>
        /// Gets Navigation Links for the top navigation menu for the given request URL path.
        /// </summary>
        /// <param name="requestUrlPath">The request URL path.</param>
        /// <param name="localization">The Localization.</param>
        /// <returns>The Navigation Links.</returns>
        public virtual NavigationLinks GetTopNavigationLinks(string requestUrlPath, Localization localization)
        {
            using (new Tracer(requestUrlPath, localization))
            {
                SitemapItem sitemapRoot = GetNavigationModel(localization);
                return new NavigationLinks
                {
                    Items = sitemapRoot.Items.Where(i => i.Visible).Select(i => RewriteIndexPage(i, sitemapRoot).CreateLink(localization)).ToList()
                };
            }
        }

        /// <summary>
        /// Gets Navigation Links for the context navigation panel for the given request URL path.
        /// </summary>
        /// <param name="requestUrlPath">The request URL path.</param>
        /// <param name="localization">The Localization.</param>
        /// <returns>The Navigation Links.</returns>
        public virtual NavigationLinks GetContextNavigationLinks(string requestUrlPath, Localization localization)
        {
            using (new Tracer(requestUrlPath, localization))
            {
                // Find the context Sitemap Item; start with Sitemap root.
                SitemapItem contextSitemapItem = GetNavigationModel(localization);
                if (requestUrlPath.Contains("/"))
                {
                    while (contextSitemapItem.Items != null)
                    {
                        SitemapItem matchingChildSg = contextSitemapItem.Items.FirstOrDefault(i => i.Type == SitemapItem.Types.StructureGroup && requestUrlPath.StartsWith(i.Url, StringComparison.InvariantCultureIgnoreCase));
                        if (matchingChildSg == null)
                        {
                            // No matching child SG found => current contextSitemapItem reflects the context SG.
                            break;
                        }
                        contextSitemapItem = matchingChildSg;
                    }
                }

                if (contextSitemapItem.Items == null)
                {
                    throw new DxaException(string.Format("Context SitemapItem has no child items: {0}", contextSitemapItem));
                }

                return new NavigationLinks
                {
                    Items = contextSitemapItem.Items.Where(i => i.Visible).Select(i => i.CreateLink(localization)).ToList()
                };
            }
        }

        /// <summary>
        /// Gets Navigation Links for the breadcrumb trail for the given request URL path.
        /// </summary>
        /// <param name="requestUrlPath">The request URL path.</param>
        /// <param name="localization">The Localization.</param>
        /// <returns>The Navigation Links.</returns>
        public virtual NavigationLinks GetBreadcrumbNavigationLinks(string requestUrlPath, Localization localization)
        {
            using (new Tracer(requestUrlPath, localization))
            {

                int levels = requestUrlPath.Split('/').Length;

                SitemapItem currentItem = GetNavigationModel(localization); // Start with Sitemap root.
                List<Link> links = new List<Link> { currentItem.CreateLink(localization) };
                while (levels > 1 && currentItem.Items != null)
                {
                    currentItem = currentItem.Items.FirstOrDefault(i => requestUrlPath.StartsWith(i.Url, StringComparison.InvariantCultureIgnoreCase));
                    if (currentItem == null)
                    {
                        break;
                    }
                    links.Add(currentItem.CreateLink(localization));
                    levels--;
                }

                return new NavigationLinks
                {
                    Items = links
                };
            }
        }
        #endregion

        private SitemapItem BuildNavigationModel(Localization localization)
        {
            using (new Tracer(localization))
            {
                string navigationJsonUrlPath = SiteConfiguration.LocalizeUrl("navigation.json", localization);

                Log.Debug("Deserializing Navigation Model from raw content URL '{0}'", navigationJsonUrlPath);
                IRawDataProvider rawDataProvider = SiteConfiguration.ContentProvider as IRawDataProvider;
                if (rawDataProvider == null)
                {
                    throw new DxaException(
                        string.Format("The current Content Provider '{0}' does not implement interface '{1}' and hence cannot be used in combination with Navigation Provider '{2}'.",
                            SiteConfiguration.ContentProvider.GetType().FullName, typeof(IRawDataProvider).FullName, GetType().FullName)
                        );
                }

                return JsonConvert.DeserializeObject<SitemapItem>(rawDataProvider.GetPageContent(navigationJsonUrlPath, localization));
            }
        }

        private static SitemapItem RewriteIndexPage(SitemapItem sitemapItem, SitemapItem parentSitemapItem)
        {
            // TODO: test on Url instead?
            return (sitemapItem.Title == "Index") ? parentSitemapItem : sitemapItem;
        }

    }
}
