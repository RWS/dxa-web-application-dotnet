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
using Sdl.Web.Common.Models.Navigation;
using Sdl.Web.Tridion.ContentManager;
using Tridion.ContentDelivery.Meta;
using Tridion.ContentDelivery.Taxonomies;
using TaxonomyFactory = Tridion.ContentDelivery.Taxonomies.TaxonomyFactory;

namespace Sdl.Web.Tridion.Navigation
{
    /// <summary>
    /// Navigation Provider implementation based on Taxonomies (Categories and Keywords)
    /// </summary>
    public class DynamicNavigationProvider : INavigationProvider, IOnDemandNavigationProvider
    {
        private const string TaxonomyNavigationMarker = "[Navigation]";
        private const string NavigationCacheRegionName = "Navigation_Dynamic";
        private const string NavTaxonomyCacheRegionName = "NavTaxonomy";
        private const string IndexPageUrlSuffix = "/index";

        private static readonly Regex _cmTitleRegex = new Regex(@"(?<sequence>\d\d\d)?\s*(?<title>.*)", RegexOptions.Compiled);
        private static readonly Regex _sitemapItemIdRegex = new Regex(@"^t(?<taxonomyId>\d+)((-k(?<keywordId>\d+))|(-p(?<pageId>\d+)))?$", RegexOptions.Compiled);
        private static readonly INavigationProvider _fallbackNavigationProvider = new StaticNavigationProvider();

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
                if (string.IsNullOrEmpty(navTaxonomyUri))
                {
                    // No Navigation Taxonomy found in this Localization; fallback to the StaticNavigationProvider.
                    return _fallbackNavigationProvider.GetNavigationModel(localization);
                }

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
                SitemapItem rootSitemapItem = GetNavigationModel(localization);
                if (!(rootSitemapItem is TaxonomyNode))
                {
                    // No Navigation Taxonomy found in this Localization; fallback to the StaticNavigationProvider.
                    _fallbackNavigationProvider.GetTopNavigationLinks(requestUrlPath, localization);
                }

                return new NavigationLinks
                {
                    Items = rootSitemapItem.Items.Where(i => i.Visible).Select(i => i.CreateLink(localization)).ToList()
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
                if (!(navModel is TaxonomyNode))
                {
                    // No Navigation Taxonomy found in this Localization; fallback to the StaticNavigationProvider.
                    _fallbackNavigationProvider.GetContextNavigationLinks(requestUrlPath, localization);
                }

                string normalizedUrlPath = ExpandIndexPageUrl(StripFileExtension(requestUrlPath));
                SitemapItem contextNode = navModel.FindSitemapItem(normalizedUrlPath);
                if (!(contextNode is TaxonomyNode))
                {
                    contextNode = contextNode.Parent;
                }

                List<Link> links = new List<Link>();
                if (contextNode != null)
                {
                    links.AddRange(contextNode.Items.Where(i => i.Visible).Select(i => i.CreateLink(localization)));
                }

                return new NavigationLinks
                {
                    Items = links
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
                if (!(navModel is TaxonomyNode))
                {
                    // No Navigation Taxonomy found in this Localization; fallback to the StaticNavigationProvider.
                    _fallbackNavigationProvider.GetBreadcrumbNavigationLinks(requestUrlPath, localization);
                }

                string normalizedUrlPath = ExpandIndexPageUrl(StripFileExtension(requestUrlPath));
                SitemapItem sitemapItem = navModel.FindSitemapItem(normalizedUrlPath);

                List<Link> breadcrumb = new List<Link>();

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
                if (string.IsNullOrEmpty(sitemapItemId))
                {
                    return ExpandTaxonomyRoots(filter, localization);
                }

                // Extract Taxonomy TCM UI, Keyword TCM URI and/or Page TCM URI from the Sitemap Item ID
                string taxonomyId;
                string keywordId;
                string pageId;
                ParseSitemapItemId(sitemapItemId, out taxonomyId, out keywordId, out pageId);
                string publicationId = localization.LocalizationId;
                string taxonomyUri = string.Format("tcm:{0}-{1}-512", publicationId, taxonomyId);
                string keywordUri = string.IsNullOrEmpty(keywordId) ?  taxonomyUri : string.Format("tcm:{0}-{1}-1024", publicationId, keywordId);
                string pageUri = string.Format("tcm:{0}-{1}-64", publicationId, pageId);

                IEnumerable<SitemapItem> result = new SitemapItem[0];
                if (filter.IncludeAncestors)
                {
                    TaxonomyNode taxonomyRoot = null;
                    if (!string.IsNullOrEmpty(keywordId))
                    {
                        taxonomyRoot = ExpandAncestorsForKeyword(keywordUri, taxonomyUri, filter, localization);
                    }
                    else if (!string.IsNullOrEmpty(pageId))
                    {
                        taxonomyRoot = ExpandAncestorsForPage(pageUri, taxonomyUri, filter, localization);
                    }

                    if (taxonomyRoot != null)
                    {
                        if (filter.DescendantLevels == 0)
                        {
                            // No descendants have been requested; ensure Items properties are null on the leaf Taxonomy Nodes.
                            PruneLeafTaxonomyNodes(taxonomyRoot);
                        }
                        else
                        {
                            AddDescendants(taxonomyRoot, filter, localization);
                        }

                        result = new[] { taxonomyRoot };
                    }
                }
                else if (filter.DescendantLevels != 0 && string.IsNullOrEmpty(pageId))
                {
                    result = ExpandDescendants(keywordUri, taxonomyUri, filter, localization);
                }

                return result;
            }
        }
        #endregion

        private static bool IsHome(SitemapItem sitemapItem, Localization localization)
        {
            string homePath = string.IsNullOrEmpty(localization.Path) ? "/" : localization.Path;
            return sitemapItem != null && sitemapItem.Url != null && sitemapItem.Url.Equals(homePath, StringComparison.InvariantCultureIgnoreCase);
        }

        private static void ParseSitemapItemId(string sitemapItemId, out string taxonomyId, out string keywordId, out string pageId)
        {
            Match sitemapItemIdMatch = _sitemapItemIdRegex.Match(sitemapItemId);
            if (!sitemapItemIdMatch.Success)
            {
                throw new DxaException(string.Format("Invalid Sitemap Item identifier: '{0}'", sitemapItemId));
            }
            taxonomyId = sitemapItemIdMatch.Groups["taxonomyId"].Value;
            keywordId = sitemapItemIdMatch.Groups["keywordId"].Value;
            pageId = sitemapItemIdMatch.Groups["pageId"].Value;
        }

        private static void PruneLeafTaxonomyNodes(TaxonomyNode taxonomyNode)
        {
            if (taxonomyNode.Items == null)
            {
                // This leaf TaxonomyNode is already pruned; nothing to do.
                return;
            }

            if (taxonomyNode.Items.Count == 0)
            {
                // Prune this leaf Taxonomy node.
                taxonomyNode.Items = null;
            }
            else
            {
                // Not a leaf; prune its child Taxonomy Nodes (recursively).
                foreach (TaxonomyNode childNode in taxonomyNode.Items.OfType<TaxonomyNode>())
                {
                    PruneLeafTaxonomyNodes(childNode);
                }
            }
        }

        private static void AddDescendants(TaxonomyNode taxonomyNode, NavigationFilter filter, Localization localization)
        {
            using (new Tracer(taxonomyNode, filter, localization))
            {
                // First recurse (depth-first)
                IList<SitemapItem> children = taxonomyNode.Items;
                foreach (TaxonomyNode childNode in children.OfType<TaxonomyNode>())
                {
                    AddDescendants(childNode, filter, localization);
                }

                // Add descendants (on the way back)
                string taxonomyId;
                string keywordId;
                string pageId;
                ParseSitemapItemId(taxonomyNode.Id, out taxonomyId, out keywordId, out pageId);
                string publicationId = localization.LocalizationId;
                string taxonomyUri = string.Format("tcm:{0}-{1}-512", publicationId, taxonomyId);
                string keywordUri = string.IsNullOrEmpty(keywordId) ? taxonomyUri : string.Format("tcm:{0}-{1}-1024", publicationId, keywordId);

                IEnumerable<SitemapItem> additionalChildren = ExpandDescendants(keywordUri, taxonomyUri, filter, localization);
                foreach (SitemapItem additionalChildItem in additionalChildren.Where(childItem => children.All(i => i.Id != childItem.Id)))
                {
                    children.Add(additionalChildItem);
                }
            }
        }

        private static IEnumerable<SitemapItem> ExpandTaxonomyRoots(NavigationFilter filter, Localization localization)
        {
            using (new Tracer(filter, localization))
            {
                TaxonomyFactory taxonomyFactory = new TaxonomyFactory();
                string[] taxonomyIds = taxonomyFactory.GetTaxonomies(GetPublicationTcmUri(localization));

                int depth= filter.DescendantLevels > 0 ? (filter.DescendantLevels - 1) : filter.DescendantLevels;
                TaxonomyFilter taxonomyFilter = new DepthFilter(depth, DepthFilter.FilterDown);
                IEnumerable<Keyword> taxonomyRoots = taxonomyIds.Select(id => taxonomyFactory.GetTaxonomyKeywords(id, taxonomyFilter));

                return taxonomyRoots.Select(kw => CreateTaxonomyNode(kw, depth, filter, localization));
            }
        }

        private static IEnumerable<SitemapItem> ExpandDescendants(string keywordUri, string taxonomyUri, NavigationFilter filter, Localization localization)
        {
            using (new Tracer(keywordUri, taxonomyUri, filter, localization))
            {
                TaxonomyFactory taxonomyFactory = new TaxonomyFactory();
                TaxonomyFilter taxonomyFilter = new DepthFilter(filter.DescendantLevels, DepthFilter.FilterDown);
                Keyword contextKeyword = taxonomyFactory.GetTaxonomyKeywords(taxonomyUri, taxonomyFilter, keywordUri);
                TaxonomyNode contextTaxonomyNode = CreateTaxonomyNode(contextKeyword, filter.DescendantLevels, filter, localization);
                return contextTaxonomyNode.Items;
            }
        }

        private static TaxonomyNode ExpandAncestorsForKeyword(string keywordUri, string taxonomyUri, NavigationFilter filter, Localization localization)
        {
            using (new Tracer(keywordUri, taxonomyUri, filter, localization))
            {
                TaxonomyFactory taxonomyFactory = new TaxonomyFactory();
                TaxonomyFilter taxonomyFilter = new DepthFilter(DepthFilter.UnlimitedDepth, DepthFilter.FilterUp);
                Keyword taxonomyRoot = taxonomyFactory.GetTaxonomyKeywords(taxonomyUri, taxonomyFilter, keywordUri);
                return CreateTaxonomyNode(taxonomyRoot, -1, filter, localization);
            }
        }

        private static TaxonomyNode ExpandAncestorsForPage(string pageUri, string taxonomyUri, NavigationFilter filter, Localization localization)
        {
            using (new Tracer(pageUri, taxonomyUri, filter, localization))
            {
                // Get TaxonomyRelationManager.GetTaxonomyKeywords may return multiple paths towards the (same) Taxonomy root.
                TaxonomyRelationManager taxonomyRelationManager = new TaxonomyRelationManager();
                Keyword[] taxonomyRoots = taxonomyRelationManager.GetTaxonomyKeywords(taxonomyUri, pageUri, null, new DepthFilter(-1, DepthFilter.FilterUp), (int) ItemType.Page);
                if (taxonomyRoots == null || taxonomyRoots.Length == 0)
                {
                    Log.Debug("No Keywords found for Page '{0}' in Taxonomy '{1}'.", pageUri, taxonomyUri);
                    return null;
                }

                TaxonomyNode[] taxonomyRootNodes = taxonomyRoots.Select(kw => CreateTaxonomyNode(kw, -1, filter, localization)).ToArray();

                // Merge all returned paths into a single subtree
                TaxonomyNode mergedSubtreeRootNode = taxonomyRootNodes[0];
                foreach (TaxonomyNode taxonomyRootNode in taxonomyRootNodes.Skip(1))
                {
                    MergeSubtrees(taxonomyRootNode, mergedSubtreeRootNode);
                }

                return mergedSubtreeRootNode;
            }
        }

        private static void MergeSubtrees(SitemapItem subtreeRoot, SitemapItem subtreeToMergeInto)
        {
            foreach (SitemapItem childNode in subtreeRoot.Items)
            {
                SitemapItem childKeywordToMergeInto = subtreeToMergeInto.Items.FirstOrDefault(i => i.Id == childNode.Id);
                if (childKeywordToMergeInto == null)
                {
                    subtreeToMergeInto.Items.Add(childNode);
                }
                else
                {
                    MergeSubtrees(childNode, childKeywordToMergeInto);
                }
            }
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
                    Log.Warn("No Navigation Taxonomy Found in Localization [{0}]. Ensure a Taxonomy with '{1}' in its title is published.", localization, TaxonomyNavigationMarker);
                    return string.Empty;
                }

                Log.Debug("Resolved Navigation Taxonomy: {0} ('{1}')", navTaxonomyRoot.TaxonomyUri, navTaxonomyRoot.KeywordName);

                return navTaxonomyRoot.TaxonomyUri;
            }
        }

        private static SitemapItem BuildNavigationModel(string navTaxonomyUri, Localization localization)
        {
            using (new Tracer(navTaxonomyUri, localization))
            {
                TaxonomyFactory taxonomyFactory = new TaxonomyFactory();
                Keyword taxonomyRoot = taxonomyFactory.GetTaxonomyKeywords(navTaxonomyUri, new DepthFilter(DepthFilter.UnlimitedDepth, DepthFilter.FilterDown));

                NavigationFilter navFilter = new NavigationFilter();
                return CreateTaxonomyNode(taxonomyRoot, -1, navFilter, localization);
            }
        }

        private static TaxonomyNode CreateTaxonomyNode(Keyword keyword, int expandLevels, NavigationFilter filter, Localization localization)
        {
            string taxonomyId = keyword.TaxonomyUri.Split('-')[1];
            bool isRoot = (keyword.KeywordUri == keyword.TaxonomyUri);
            int classifiedItemsCount = keyword.ReferencedContentCount;
            string taxonomyNodeUrl = null;

            List<SitemapItem> childItems = null;
            if (expandLevels != 0)
            {
                childItems = new List<SitemapItem>();

                // Add child SitemapItems for child Taxonomy Nodes (ordered by title, including sequence prefix if any)
                TaxonomyNode[] childTaxonomyNodes = keyword.KeywordChildren.Cast<Keyword>()
                    .Select(kw => CreateTaxonomyNode(kw, expandLevels - 1, filter, localization))
                    .OrderBy(tn => tn.Title)
                    .ToArray();
                StripTitleSequencePrefixes(childTaxonomyNodes);
                childItems.AddRange(childTaxonomyNodes);

                if (classifiedItemsCount > 0 && filter.DescendantLevels != 0)
                {
                    // Add child SitemapItems for classified Pages (ordered by title)
                    SitemapItem[] pageSitemapItems = ExpandClassifiedPages(keyword, taxonomyId, localization);
                    childItems.AddRange(pageSitemapItems);

                    // If the Taxonomy Node contains an Index Page (i.e. URL path ending with "/index"), we put the Page's SG URL on the Taxonomy Node.
                    string indexPageUrlPath = pageSitemapItems.Select(i => i.Url).FirstOrDefault(url => url.EndsWith(IndexPageUrlSuffix));
                    if (indexPageUrlPath != null)
                    {
                        // Strip off "/index" URL suffix so we get the Page's SG URL (except for the Site Home Page, where we use "/")
                        taxonomyNodeUrl = (indexPageUrlPath == IndexPageUrlSuffix) ? "/" :
                            indexPageUrlPath.Substring(0, indexPageUrlPath.Length - IndexPageUrlSuffix.Length);
                    }
                }
            }

            string sequencePrefix = _cmTitleRegex.Match(keyword.KeywordName).Groups["sequence"].Value; 

            TaxonomyNode result = new TaxonomyNode
            {
                Id = isRoot ? string.Format("t{0}", taxonomyId) : FormatKeywordNodeId(keyword.KeywordUri, taxonomyId),
                Type =  SitemapItem.Types.TaxonomyNode,
                Title = keyword.KeywordName,
                Url = taxonomyNodeUrl,
                Visible = !string.IsNullOrEmpty(sequencePrefix) && !string.IsNullOrEmpty(taxonomyNodeUrl),
                Items = childItems,
                Key = keyword.KeywordKey,
                Description = keyword.KeywordDescription,
                IsAbstract = keyword.IsAbstract,
                HasChildNodes = keyword.HasChildren || (classifiedItemsCount > 0),
                ClassifiedItemsCount = classifiedItemsCount,
                RelatedTaxonomyNodeIds = filter.IncludeRelated ? keyword.GetRelatedKeywordUris().Select(uri => FormatKeywordNodeId(uri, taxonomyId)).ToList() : null,
                CustomMetadata = filter.IncludeCustomMetadata ? GetCustomMetadata(keyword) : null
            };

            if (childItems != null)
            {
                foreach (SitemapItem childItem in childItems)
                {
                    childItem.Parent = result;
                }
            }

            return result;
        }

        private static IDictionary<string, object> GetCustomMetadata(Keyword keyword)
        {
            throw new System.NotImplementedException(); // TODO
        }

        private static SitemapItem[] ExpandClassifiedPages(Keyword keyword, string taxonomyId, Localization localization)
        {
            using (new Tracer(keyword.KeywordUri, taxonomyId, localization))
            {
                // Return SitemapItems for all classified Pages (ordered by Page Title, including sequence prefix if any)
                PageMetaFactory pageMetaFactory = new PageMetaFactory(GetPublicationTcmUri(localization));
                IPageMeta[] classifiedPageMetas = pageMetaFactory.GetTaxonomyPages(keyword, includeBranchedFacets: false);
                SitemapItem[] result = classifiedPageMetas.Select(pageMeta => CreateSitemapItem(pageMeta, taxonomyId)).OrderBy(i => i.Title).ToArray();

                StripTitleSequencePrefixes(result);

                return result;
            }
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

        private static void StripTitleSequencePrefixes(IEnumerable<SitemapItem> sitemapItems)
        {
            foreach (SitemapItem sitemapItem in sitemapItems)
            {
                sitemapItem.Title = _cmTitleRegex.Match(sitemapItem.Title).Groups["title"].Value;
            }
        }

        private static string StripFileExtension(string urlPath)
        {
            if (urlPath.EndsWith(Constants.DefaultExtension))
            {
                urlPath = urlPath.Substring(0, urlPath.Length - Constants.DefaultExtension.Length);
            }
            return urlPath;
        }

        private static string ExpandIndexPageUrl(string urlPath)
        {
            if (urlPath.EndsWith("/"))
            {
                urlPath += "index";
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
