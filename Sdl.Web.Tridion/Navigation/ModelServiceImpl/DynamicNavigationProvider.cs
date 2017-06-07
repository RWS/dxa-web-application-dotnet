using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Navigation;
using Sdl.Web.Tridion.R2Mapping;

namespace Sdl.Web.Tridion.Navigation.ModelServiceImpl
{
    /// <summary>
    /// Navigation Provider implementation based on Taxonomies (Categories and Keywords)
    /// This implementation uses the model service to construct the sitemap items.
    /// </summary>
    public class DynamicNavigationProvider : INavigationProvider, IOnDemandNavigationProvider
    {
        private readonly ModelServiceClient _modelService = new ModelServiceClient();
        private static readonly INavigationProvider FallbackNavigationProvider = new StaticNavigationProvider();
        private static readonly Regex SitemapItemIdRegex = new Regex(@"^t(?<taxonomyId>\d+)((-k(?<keywordId>\d+))|(-p(?<pageId>\d+)))?$", RegexOptions.Compiled);
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
                var cachedNavModel = SiteConfiguration.CacheProvider.GetOrAdd(
                    $"GetNavigationModel:{localization.Id}",
                    CacheRegions.DynamicNavigation,
                    () =>
                    {
                        var navModel = _modelService.GetSitemapItem(localization) ??
                                       FallbackNavigationProvider.GetNavigationModel(localization);
                        RebuildParentRelationships(navModel.Items, navModel);
                        return navModel;
                    }
                    );

                if (cachedNavModel != null && cachedNavModel.Items.Count > 0 &&
                    cachedNavModel.Items.First().Parent == null)
                    RebuildParentRelationships(cachedNavModel.Items, cachedNavModel);

                return cachedNavModel;
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
                var navModel = GetNavigationModel(localization);
                if (navModel == null)
                    return null;

                return SiteConfiguration.CacheProvider.GetOrAdd(
                    $"GetTopNavigationLinks:{requestUrlPath}-{localization.Id}",
                    CacheRegions.DynamicNavigation,
                    () => new NavigationLinks
                    {
                        Items =
                            navModel.Items.Where(i => i.Visible)
                                .Select(i => i.CreateLink(localization))
                                .ToList()
                    });
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
                var navModel = GetNavigationModel(localization);
                if (navModel == null)
                    return null;
                return SiteConfiguration.CacheProvider.GetOrAdd(
                    $"GetContextNavigationLinks:{requestUrlPath}-{localization.Id}",
                    CacheRegions.DynamicNavigation,
                    () =>
                    {
                        SitemapItem contextNode = navModel.FindSitemapItem(requestUrlPath.NormalizePageUrlPath());
                        if (contextNode != null && !(contextNode is TaxonomyNode))
                        {
                            contextNode = contextNode.Parent;
                        }

                        List<Link> links = new List<Link>();
                        if (contextNode != null)
                        {
                            links.AddRange(
                                contextNode.Items.Where(i => i.Visible).Select(i => i.CreateLink(localization)));
                        }

                        return new NavigationLinks
                        {
                            Items = links
                        };
                    });
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
                var navModel = GetNavigationModel(localization);
                if (navModel == null)
                    return null;
                return SiteConfiguration.CacheProvider.GetOrAdd(
                    $"GetBreadcrumbNavigationLinks:{requestUrlPath}-{localization.Id}",
                    CacheRegions.DynamicNavigation,
                    () =>
                    {
                        List<Link> breadcrumb = new List<Link>();
                        SitemapItem sitemapItem = navModel.FindSitemapItem(requestUrlPath.NormalizePageUrlPath());

                        if (sitemapItem != null)
                        {
                            // Build a breadcrumb of ancestors, excluding the Taxonomy Root.
                            bool hasHome = false;
                            while (sitemapItem.Parent != null)
                            {
                                breadcrumb.Insert(0, sitemapItem.CreateLink(localization));
                                hasHome = IsHome(sitemapItem, localization);
                                sitemapItem = sitemapItem.Parent;
                            }

                            // The Home TaxonomyNode/Keyword may be a top-level sibling instead of an ancestor
                            if (!hasHome)
                            {
                                SitemapItem home = sitemapItem.Items.FirstOrDefault(i => IsHome(i, localization));
                                if (home != null)
                                {
                                    breadcrumb.Insert(0, home.CreateLink(localization));
                                }
                            }
                        }

                        return new NavigationLinks
                        {
                            Items = breadcrumb
                        };
                    });
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
                if (!string.IsNullOrEmpty(sitemapItemId))
                {
                    Match sitemapItemIdMatch = SitemapItemIdRegex.Match(sitemapItemId);
                    if (!sitemapItemIdMatch.Success)
                    {
                        throw new DxaException($"Invalid Sitemap Item identifier: '{sitemapItemId}'");
                    }
                }

                if (filter == null)
                {   // default
                    filter = new NavigationFilter {DescendantLevels = 1, IncludeAncestors = false};
                }
                IEnumerable<SitemapItem> cachedNavModel = SiteConfiguration.CacheProvider.GetOrAdd(
                    $"GetNavigationSubtree:{sitemapItemId}-{localization.Id}-{filter.IncludeAncestors}-{filter.DescendantLevels}",
                    CacheRegions.DynamicNavigation,
                    () =>
                    {
                        try
                        {
                            IEnumerable<SitemapItem> items = _modelService.GetChildSitemapItems(sitemapItemId, localization,
                                filter.IncludeAncestors,
                                filter.DescendantLevels) ?? new SitemapItem[0];
                            items = items.OrderBy(i => i.OriginalTitle);
                            RebuildParentRelationships(items, null);
                            return items;
                        }
                        catch
                        {
                            return new SitemapItem[0];
                        }                      
                    });

                if (cachedNavModel == null) return null;
                RebuildParentRelationships(cachedNavModel, null);
                return cachedNavModel;
            }
        }
        #endregion      

        private static void RebuildParentRelationships(IEnumerable<SitemapItem> children, SitemapItem parent)
        {
            if (children == null) return;
            foreach (var child in children)
            {
                child.Parent = parent;
                child.Items = child.Items.OrderBy(i => i.OriginalTitle).ToList();
                RebuildParentRelationships(child.Items, child);
            }
        }

        private static bool IsHome(SitemapItem sitemapItem, Localization localization)
        {
            string homePath = string.IsNullOrEmpty(localization.Path) ? "/" : localization.Path;
            return (sitemapItem?.Url != null) && sitemapItem.Url.Equals(homePath, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
