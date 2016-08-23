using System;
using System.Collections.Generic;
using System.Linq;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Entity;
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
        private const string SitemapItemTypeCategory = "Category";
        private const string SitemapItemTypeKeyword = "Keyword";
        private const string SitemapItemTypePage = "Page";

        #region INavigationProvider members
        public SitemapItem GetNavigationModel(Localization localization)
        {
            using (new Tracer(localization))
            {
                string navTaxonomyId = GetNavigationTaxonomyId(localization);

                TaxonomyFactory taxonomyFactory = new TaxonomyFactory();
                TaxonomyFilter wholeTaxonomyFilter = new DepthFilter(-1, DepthFilter.FilterDown);
                Keyword taxonomyRoot = taxonomyFactory.GetTaxonomyKeywords(navTaxonomyId, wholeTaxonomyFilter);

                return CreateTaxonomyNode(taxonomyRoot, localization);
            }
        }

        public NavigationLinks GetTopNavigationLinks(string requestUrlPath, Localization localization)
        {
            using (new Tracer(requestUrlPath, localization))
            {
                throw new NotImplementedException(); // TODO
            }
        }

        public NavigationLinks GetContextNavigationLinks(string requestUrlPath, Localization localization)
        {
            using (new Tracer(requestUrlPath, localization))
            {
                throw new NotImplementedException(); // TODO
            }
        }

        public NavigationLinks GetBreadcrumbNavigationLinks(string requestUrlPath, Localization localization)
        {
            using (new Tracer(requestUrlPath, localization))
            {
                throw new NotImplementedException(); // TODO
            }
        }
        #endregion

        private string GetNavigationTaxonomyId(Localization localization)
        {
            using (new Tracer(localization))
            {
                // TODO PERF: this is rather expensive; cache it somehow.
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
            string keywordId = keyword.KeywordUri.Split('-')[1];
            bool isRoot = (keyword.KeywordUri == keyword.TaxonomyUri);


            List<SitemapItem> childItems = new List<SitemapItem>();

            // Add child SitemapItems for child Taxonomy Nodes
            IEnumerable<TaxonomyNode> childTaxonomyNodes = keyword.KeywordChildren.Cast<Keyword>().Select(kw => CreateTaxonomyNode(kw, localization));
            childItems.AddRange(childTaxonomyNodes);

            if (keyword.ReferencedContentCount > 0)
            {
                // Add child SitemapItems for classified Pages
                PageMetaFactory pageMetaFactory = new PageMetaFactory(GetPublicationTcmUri(localization));
                IPageMeta[] classifiedPageMetas = pageMetaFactory.GetTaxonomyPages(keyword, includeBranchedFacets: false);
                IEnumerable<SitemapItem> pageSitemapItems = classifiedPageMetas.Select(pageMeta => CreateSitemapItem(pageMeta, taxonomyId));
                childItems.AddRange(pageSitemapItems);
            }

            return new TaxonomyNode
            {
                Id = isRoot ? string.Format("t{0}", taxonomyId) : string.Format("t{0}-k{1}", taxonomyId, keywordId),
                Type =  isRoot ? SitemapItemTypeCategory : SitemapItemTypeKeyword,
                Title = keyword.KeywordName,
                Visible = true,
                Items = childItems,
                Key = keyword.KeywordKey,
                Description = keyword.KeywordDescription,
                IsAbstract = keyword.IsAbstract,
                ClassifiedItemsCount = keyword.ReferencedContentCount
            };

            // TODO: RelatedTaxonomyNodeIds, CustomMetadata
        }

        private static SitemapItem CreateSitemapItem(IPageMeta pageMeta, string taxonomyId)
        {
            return new SitemapItem
            {
                Id = string.Format("t{0}-p{1}", taxonomyId, pageMeta.Id),
                Type = SitemapItemTypePage,
                Title = pageMeta.Title,
                Url = pageMeta.UrlPath,
                Visible = true
            };
        }

        private static string GetPublicationTcmUri(Localization localization)
        {
            return string.Format("tcm:0-{0}-1", localization.LocalizationId);
        }
    }
}
