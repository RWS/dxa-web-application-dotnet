using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Entity;
using Sdl.Web.Tridion.ContentManager;
using Tridion.ContentDelivery.Meta;
using Tridion.ContentDelivery.Taxonomies;
using TaxonomyFactory = Tridion.ContentDelivery.Taxonomies.TaxonomyFactory;

namespace Sdl.Web.Tridion.Navigation
{
    /// <summary>
    /// Navigation Provider implementation based on Taxonomies (Categories & Keywords)
    /// </summary>
    public class DynamicNavigationProvider : INavigationProvider
    {
        private const string TaxonomyNavigationMarker = "[Navigation]";
        private const string NavigationCacheRegionName = "Navigation";
        private const string NavTaxonomyCacheRegionName = "NavTaxonomy";
        private const string IndexPageUrlSuffix = "/index";

        private static readonly Regex _pageTitleRegex = new Regex(@"(?<sequence>\d\d\d)?\s*(?<title>.*)", RegexOptions.Compiled);

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
                string navTaxonomyUri = GetNavigationTaxonomyUri(localization);

                return SiteConfiguration.CacheProvider.GetOrAdd(
                    localization.LocalizationId, // key
                    NavigationCacheRegionName, 
                    () => BuildNavigationModel(navTaxonomyUri, localization), 
                    new [] { navTaxonomyUri } // dependency on Taxonomy
                    );
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
                SitemapItem rootNode = GetNavigationModel(localization);

                return new NavigationLinks
                {
                    Items = rootNode.Items.Where(i => i.Visible && !string.IsNullOrEmpty(i.Url)).Select(i => i.CreateLink(localization)).ToList()
                };
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
                SitemapItem navModel = GetNavigationModel(localization);
                SitemapItem pageSitemapItem = navModel.FindSitemapItem(StripFileExtension(requestUrlPath));
                if (pageSitemapItem == null || pageSitemapItem.Parent == null)
                {
                    return null; // TODO
                }

                return new NavigationLinks
                {
                    Items = pageSitemapItem.Parent.Items.Where(i => i.Visible && !string.IsNullOrEmpty(i.Url)).Select(i => i.CreateLink(localization)).ToList()
                };
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
                SitemapItem navModel = GetNavigationModel(localization);
                SitemapItem sitemapItem = navModel.FindSitemapItem(StripFileExtension(requestUrlPath));

                List<Link> breadcrumb = new List<Link>();
                while (sitemapItem != null)
                {
                    if (sitemapItem.Visible)
                    {
                        breadcrumb.Insert(0, sitemapItem.CreateLink(localization));
                    }
                    sitemapItem = sitemapItem.Parent;
                }

                return new NavigationLinks
                {
                    Items = breadcrumb
                };
            }
        }
        #endregion


        private SitemapItem BuildNavigationModel(string navTaxonomyUri, Localization localization)
        {
            using (new Tracer(navTaxonomyUri, localization))
            {
                TaxonomyFactory taxonomyFactory = new TaxonomyFactory();
                Keyword taxonomyRoot = taxonomyFactory.GetTaxonomyKeywords(navTaxonomyUri, new DepthFilter(DepthFilter.UnlimitedDepth, DepthFilter.FilterDown));

                return CreateTaxonomyNode(taxonomyRoot, localization);
            }
        }


        private static IEnumerable<Keyword> GetTaxonomyKeywordsForPage(string pageUri, string taxonomyUri, int depth = -1)
        {

            // TODO: Tridion.ContentDelivery.Taxonomies.TaxonomyRelationManager is missing in CIL 8.2. See CRQ-2380.
#if TRIDION_71
            global::Tridion.ContentDelivery.Taxonomies.TaxonomyRelationManager taxonomyRelationManager = new global::Tridion.ContentDelivery.Taxonomies.TaxonomyRelationManager();
            return taxonomyRelationManager.GetTaxonomyKeywords(taxonomyUri, pageUri, null, new DepthFilter(depth, DepthFilter.FilterUp), (int) ItemType.Page);
#else
            Sdl.Web.Delivery.Dynamic.Taxonomies.Filters.ITaxonomyFilter ancestorsFilter = new Sdl.Web.Delivery.Dynamic.Taxonomies.Filters.DepthFilter(depth, DepthFilter.FilterUp);
            Sdl.Web.Delivery.Dynamic.Taxonomies.TaxonomyRelationManager taxonomyRelationManager = new Sdl.Web.Delivery.Dynamic.Taxonomies.TaxonomyRelationManager();
            IEnumerable<Sdl.Web.Delivery.Model.Taxonomies.IKeyword> keywords = taxonomyRelationManager.GetTaxonomyKeywords(taxonomyUri, pageUri, null, ancestorsFilter, (int) ItemType.Page);

            ConstructorInfo wrapConstructor = typeof(Keyword).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Sdl.Web.Delivery.Model.Taxonomies.IKeyword) }, null);
            return keywords.Select(k => (Keyword) wrapConstructor.Invoke(new object[] {k})).ToArray();
#endif
        }

        private static string GetNavigationTaxonomyUri(Localization localization)
        {
            return SiteConfiguration.CacheProvider.GetOrAdd(
                localization.LocalizationId, // key
                NavTaxonomyCacheRegionName,
                () => ResolveNavigationTaxonomyUri(localization)
                );
        }

        private static string ResolveNavigationTaxonomyUri(Localization localization)
        {
            using (new Tracer(localization))
            {
                TaxonomyFactory taxonomyFactory = new TaxonomyFactory();
                string[] taxonomyIds = taxonomyFactory.GetTaxonomies(GetPublicationTcmUri(localization));

                Keyword navTaxonomyRoot = taxonomyIds.Select(id => taxonomyFactory.GetTaxonomyKeyword(id)).FirstOrDefault(tax => tax.KeywordName.Contains(TaxonomyNavigationMarker));
                if (navTaxonomyRoot == null)
                {
                    throw new DxaException(
                        string.Format("No Navigation Taxonomy Found in Localization [{0}]. Ensure a Taxonomy with '{1}' in its title is published.",
                            localization, TaxonomyNavigationMarker)
                        );
                }

                Log.Debug("Resolved Navigation Taxonomy: {0} ('{1}')", navTaxonomyRoot.TaxonomyUri, navTaxonomyRoot.KeywordName);

                return navTaxonomyRoot.TaxonomyUri;
            }
        }

        private static TaxonomyNode CreateTaxonomyNode(Keyword keyword, Localization localization)
        {
            string taxonomyId = keyword.TaxonomyUri.Split('-')[1];
            bool isRoot = (keyword.KeywordUri == keyword.TaxonomyUri);
            List<SitemapItem> childItems = new List<SitemapItem>();

            // Add child SitemapItems for child Taxonomy Nodes
            IEnumerable<TaxonomyNode> childTaxonomyNodes = keyword.KeywordChildren.Cast<Keyword>().Select(kw => CreateTaxonomyNode(kw, localization));
            childItems.AddRange(childTaxonomyNodes);

            string taxonomyNodeUrl = null;
            int classifiedItemsCount = keyword.ReferencedContentCount;
            if (classifiedItemsCount > 0)
            {
                // Add child SitemapItems for classified Pages (ordered by title)
                PageMetaFactory pageMetaFactory = new PageMetaFactory(GetPublicationTcmUri(localization));
                IPageMeta[] classifiedPageMetas = pageMetaFactory.GetTaxonomyPages(keyword, includeBranchedFacets: false);
                IEnumerable<SitemapItem> pageSitemapItems = classifiedPageMetas.Select(pageMeta => CreateSitemapItem(pageMeta, taxonomyId)).OrderBy(i => i.Title).ToArray();
                childItems.AddRange(pageSitemapItems);

                // Supress sequence prefixes from titles
                foreach (SitemapItem pageSitemapItem in pageSitemapItems)
                {
                    pageSitemapItem.Title = _pageTitleRegex.Match(pageSitemapItem.Title).Groups["title"].Value;
                }

                // If the Taxonomy Node contains an Index Page (i.e. URL ending with "/index"), we put that URL on the Taxonomy Node.
                SitemapItem indexPageSitemapItem = pageSitemapItems.FirstOrDefault(i => i.Url.EndsWith(IndexPageUrlSuffix));
                if (indexPageSitemapItem != null)
                {
                    taxonomyNodeUrl = indexPageSitemapItem.Url;
                }
            }

            List<string> relatedTaxonomyNodeIds = keyword.GetRelatedKeywordUris().Select(uri => FormatKeywordNodeId(uri, taxonomyId)).ToList();

            TaxonomyNode result = new TaxonomyNode
            {
                Id = isRoot ? string.Format("t{0}", taxonomyId) : FormatKeywordNodeId(keyword.KeywordUri, taxonomyId),
                Type =  SitemapItem.Types.TaxonomyNode,
                Title = keyword.KeywordName,
                Url = taxonomyNodeUrl,
                Visible = !isRoot,
                Items = childItems,
                Key = keyword.KeywordKey,
                Description = keyword.KeywordDescription,
                IsAbstract = keyword.IsAbstract,
                HasChildNodes = keyword.HasChildren || (classifiedItemsCount > 0),
                ClassifiedItemsCount = classifiedItemsCount,
                RelatedTaxonomyNodeIds = relatedTaxonomyNodeIds.Any() ? relatedTaxonomyNodeIds : null
                // TODO: CustomMetadata (?)
            };

            foreach (SitemapItem childItem in childItems)
            {
                childItem.Parent = result;
            }

            return result;
        }


        private static SitemapItem CreateSitemapItem(IPageMeta pageMeta, string taxonomyId)
        {
            return new SitemapItem
            {
                Id = string.Format("t{0}-p{1}", taxonomyId, pageMeta.Id),
                Type = SitemapItem.Types.Page,
                Title = pageMeta.Title,
                Url = StripFileExtension(pageMeta.UrlPath) ,
                PublishedDate = pageMeta.LastPublicationDate,
                Visible = true
            };
        }

        private static string StripFileExtension(string urlPath)
        {
            if (urlPath.EndsWith(Constants.DefaultExtension))
            {
                urlPath = urlPath.Substring(0, urlPath.Length - Constants.DefaultExtension.Length);
            }
            return urlPath;
        }

        private static string GetPublicationTcmUri(Localization localization)
        {
            return string.Format("tcm:0-{0}-1", localization.LocalizationId);
        }

        private static string FormatKeywordNodeId(string keywordUri, string taxonomyId)
        {
            string keywordId = keywordUri.Split('-')[1];
            return string.Format("t{0}-k{1}", taxonomyId, keywordId);
        }
    }
}
