using System;
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
                string url = SiteConfiguration.LocalizeUrl("navigation.json", localization);
                // TODO TSI-110: This is a temporary measure to cache the Navigation Model per request to not retrieve and serialize 3 times per request. Comprehensive caching strategy pending
                string cacheKey = "navigation-" + url;
                HttpContext httpContext = HttpContext.Current;
                if (httpContext != null && httpContext.Items[cacheKey] != null)
                {
                    Log.Debug("Obtained Navigation Model from cache.");
                    return (SitemapItem) HttpContext.Current.Items[cacheKey];
                }

                Log.Debug("Deserializing Navigation Model from raw content URL '{0}'", url);
                IRawDataProvider rawDataProvider = SiteConfiguration.ContentProvider as IRawDataProvider;
                if (rawDataProvider == null)
                {
                    throw new DxaException(
                        string.Format("The current Content Provider '{0}' does not implement interface '{1}' and hence cannot be used in combination with Navigation Provider '{2}'.",
                            SiteConfiguration.ContentProvider.GetType().FullName, typeof(IRawDataProvider).FullName, GetType().FullName)
                        );
                }

                string navigationJsonString = rawDataProvider.GetPageContent(url, localization);
                SitemapItem result = JsonConvert.DeserializeObject<SitemapItem>(navigationJsonString);

                if (httpContext != null)
                {
                    httpContext.Items[cacheKey] = result;
                }

                return result;
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
                NavigationLinks navigationLinks = new NavigationLinks();
                SitemapItem sitemapRoot = GetNavigationModel(localization);
                foreach (SitemapItem item in sitemapRoot.Items.Where(i => i.Visible))
                {
                    navigationLinks.Items.Add(CreateLink((item.Title == "Index") ? sitemapRoot : item));
                }
                return navigationLinks;
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
                NavigationLinks navigationLinks = new NavigationLinks();
                SitemapItem sitemapItem = GetNavigationModel(localization); // Start with Sitemap root Item.
                int levels = requestUrlPath.Split('/').Length;
                while (levels > 1 && sitemapItem.Items != null)
                {
                    SitemapItem newParent = sitemapItem.Items.FirstOrDefault(i => i.Type == "StructureGroup" && requestUrlPath.StartsWith(i.Url, StringComparison.InvariantCultureIgnoreCase));
                    if (newParent == null)
                    {
                        break;
                    }
                    sitemapItem = newParent;
                }

                if (sitemapItem != null && sitemapItem.Items != null)
                {
                    foreach (SitemapItem item in sitemapItem.Items.Where(i => i.Visible))
                    {
                        navigationLinks.Items.Add(CreateLink(item));
                    }
                }

                return navigationLinks;
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

                NavigationLinks navigationLinks = new NavigationLinks();
                int levels = requestUrlPath.Split('/').Length;
                SitemapItem sitemapItem = GetNavigationModel(localization); // Start with Sitemap root Item.
                navigationLinks.Items.Add(CreateLink(sitemapItem));
                while (levels > 1 && sitemapItem.Items != null)
                {
                    sitemapItem = sitemapItem.Items.FirstOrDefault(i => requestUrlPath.StartsWith(i.Url, StringComparison.InvariantCultureIgnoreCase));
                    if (sitemapItem != null)
                    {
                        navigationLinks.Items.Add(CreateLink(sitemapItem));
                        levels--;
                    }
                    else
                    {
                        break;
                    }
                }
                return navigationLinks;
            }
        }

        #endregion

        /// <summary>
        /// Creates a Link Entity Model out of a SitemapItem Entity Model.
        /// </summary>
        /// <param name="sitemapItem">The SitemapItem Entity Model.</param>
        /// <returns>The Link Entity Model.</returns>
        protected static Link CreateLink(SitemapItem sitemapItem)
        {
            string url = sitemapItem.Url;
            if (url.StartsWith("tcm:"))
            {
                url = SiteConfiguration.LinkResolver.ResolveLink(url);
            }
            return new Link
            {
                Url = url,
                LinkText = sitemapItem.Title
            };
        }
    }
}
