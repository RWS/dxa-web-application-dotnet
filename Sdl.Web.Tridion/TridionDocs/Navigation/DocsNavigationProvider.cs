using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Navigation;
using Tridion.ContentDelivery.Taxonomies;
using Sdl.Web.PublicContentApi.Utils;

namespace Sdl.Web.Tridion.TridionDocs.Navigation
{
    /// <summary>
    /// Navigation Provider for Docs
    /// TODO: This should be using the PCA client version and not the CIL
    /// </summary>
    public class DocsNavigationProvider : Tridion.Navigation.CILImpl.DynamicNavigationProvider
    {
        private static readonly Regex RegEx = new Regex("^(?:\\w)(\\d+)(?:-\\w)(\\d+)", RegexOptions.Compiled);

        public string GetBaseUrl()
        {
            var request = HttpContext.Current.Request;
            var appUrl = HttpRuntime.AppDomainAppVirtualPath;

            if (appUrl != "/")
                appUrl = "/" + appUrl;

            var baseUrl = $"{request.Url.Scheme}://{request.Url.Authority}{appUrl}";

            return baseUrl;
        }


        protected override List<SitemapItem> SortTaxonomyNodes(IList<SitemapItem> taxonomyNodes)
            // Sort by topic id since the base impl sorts alphabetically using the title
            => taxonomyNodes.OrderBy(x => int.Parse(RegEx.Match(x.Id).Groups[1].Value)).
            ThenBy(x => int.Parse(RegEx.Match(x.Id).Groups[2].Value)).ToList();

        protected override TaxonomyNode CreateTaxonomyNode(Keyword keyword, int expandLevels, NavigationFilter filter, ILocalization localization)
        {
            TaxonomyNode node = base.CreateTaxonomyNode(keyword, expandLevels, filter, localization);
            string ishRefUri = (string)keyword.KeywordMeta.GetFirstValue("ish.ref.uri");
            if (ishRefUri != null)
            {
                var ish = CmUri.FromString(ishRefUri);
                node.Url = $"/{ish.PublicationId}/{ish.ItemId}";
            }
            node.Visible = true;
            return node;
        }

        protected override IEnumerable<SitemapItem> ExpandDescendants(string keywordUri, string taxonomyUri,
            NavigationFilter filter, ILocalization localization)
        {
            TaxonomyFactory taxonomyFactory = new TaxonomyFactory();
            TaxonomyFilter taxonomyFilter = new DepthFilter(filter.DescendantLevels, DepthFilter.FilterDown);
            Keyword contextKeyword = taxonomyFactory.GetTaxonomyKeywords(taxonomyUri, taxonomyFilter, keywordUri);
            if (contextKeyword == null)
            {
                throw new DxaItemNotFoundException($"Keyword for taxonomy {taxonomyUri}-{keywordUri} not found");
            }

            TaxonomyNode contextTaxonomyNode = CreateTaxonomyNode(contextKeyword, filter.DescendantLevels, filter,
                localization);
            return contextTaxonomyNode.Items;
        }

        protected override SitemapItem[] ExpandClassifiedPages(Keyword keyword, string taxonomyId,
            ILocalization localization)
            => new SitemapItem[] { };       
    }
}
