using System;
using System.Collections.Generic;
using System.Linq;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Tridion.Linking;
using Sdl.Web.Tridion.Mapping;
using Sdl.Web.Tridion.Navigation;
using Sdl.Web.Tridion.Caching;

namespace Sdl.Web.Tridion.Tests
{
    internal class TestFixture : ILocalizationResolver
    {
        internal const string HomePageId = "640";
        internal const string ArticleDcpEntityId = "9712-9711";
        internal const string ArticlePageUrlPath = "/autotest-parent-legacy/test_article_page.html";
        internal const string ArticleChildPageUrlPath = "/autotest-child-legacy/test_article_page.html";
        internal const string ArticleDynamicPageUrlPath = "/autotest-parent-legacy/test_article_dynamic.html";
        internal const string MediaManagerTestPageUrlPath = "/autotest-parent-legacy/mm_test.html";
        internal const string Tsi1278PageUrlPath = "/autotest-parent-legacy/tsi-1278_trådløst.html";
        internal const string Tsi1278StaticContentItemUrlPath = "/autotest-parent-legacy/Images/trådløst_tcm1081-9791.jpg";
        internal const string Tsi1614PageUrlPath = "/autotest-parent-legacy/tsi-1614.html";
        internal const string Tsi1758PageUrlPath = "/autotest-parent-legacy/regression/tsi-1758.html";
        internal const string Tsi1852PageUrlPath = "/autotest-parent-legacy/regression/tsi-1852.html";
        internal const string Tsi1946PageUrlPath = "/autotest-parent-legacy/regression/tsi-1946.html";
        internal const string Tsi811PageUrlPath = "/autotest-parent-legacy/regression/tsi-811";
        internal const string Tsi1308PageUrlPath = "/autotest-parent-legacy/regression/tsi-1308";
        internal const string Tsi1757PageUrlPath = "/autotest-child-legacy/regression/tsi-1757";
        internal const string Tsi2225PageUrlPath = "/autotest-parent-legacy/regression/tsi-2225";
        internal const string TaxonomyTestPage1UrlPath = "/autotest-parent-legacy/regression/taxonomy/nav-taxonomy-test-1.html";
        internal const string TaxonomyTestPage2UrlPath = "/autotest-parent-legacy/regression/taxonomy/nav-taxonomy-test-2.html";
        internal const string TaxonomyIndexPageUrlPath = "/autotest-parent-legacy/regression/taxonomy";
        internal const string NavigationTaxonomyTitle = "Test Taxonomy [Navigation]";
        internal const string TopLevelKeyword1Title = "Top-level Keyword 1";
        internal const string TopLevelKeyword2Title = "Top-level Keyword 2";
        internal const string Keyword1_1Title = "Keyword 1.1";
        internal const string Keyword1_2Title = "Keyword 1.2";

        private static readonly IEnumerable<Localization> _testLocalizations;
        private static readonly Localization _parentLocalization;
        private static readonly Localization _childLocalization;

        private static readonly IDictionary<Type, object> _testProviders = new Dictionary<Type, object>
        {
            { typeof(ICacheProvider), new DefaultCacheProvider() },
            { typeof(IContentProvider), new DefaultContentProvider() },
            { typeof(INavigationProvider), new StaticNavigationProvider() },
            { typeof(ILinkResolver), new DefaultLinkResolver() },
            { typeof(IRichTextProcessor), new DefaultRichTextProcessor() },
            { typeof(IMediaHelper), new MockMediaHelper() },
            { typeof(ILocalizationResolver), new TestFixture() },
            { typeof(IContextClaimsProvider), new TestContextClaimsProvider() },
            { typeof(IConditionalEntityEvaluator), new MockConditionalEntityEvaluator() }
        };

        static TestFixture()
        {
            // TODO: Retrieve Localization Info from CM (?)

            _parentLocalization = new Localization
            {
                LocalizationId = "1081",
                Path = "/autotest-parent-legacy"
            };

            _childLocalization = new Localization
            {
                LocalizationId = "1083",
                Path = "/autotest-child-legacy"
            };

            _testLocalizations = new[] { _parentLocalization, _childLocalization };

            TestRegistration.RegisterViewModels();
        }

        internal static Localization ParentLocalization
        {
            get
            {
                _parentLocalization.EnsureInitialized();
                return _parentLocalization;
            }
        }

        internal static Localization ChildLocalization
        {
            get
            {
                _childLocalization.EnsureInitialized();

                // Trick to allow us to test on a "Live" (not XPM-enabled) configuration even though we're actually on a Staging CD Environment:
                _childLocalization.IsStaging = false;
                return _childLocalization;
            }
        }

        internal static void InitializeProviders()
        {
            SiteConfiguration.InitializeProviders(interfaceType =>
            {
                object provider;
                _testProviders.TryGetValue(interfaceType, out provider);
                return provider;
            });
        }

        #region ILocalizationResolver members
        public Localization ResolveLocalization(Uri url)
        {
            throw new NotImplementedException();
        }

        public Localization GetLocalization(string localizationId)
        {
            Localization result = _testLocalizations.FirstOrDefault(loc => loc.LocalizationId == localizationId);
            if (result == null)
            {
                throw new DxaUnknownLocalizationException("Unknown Localization ID: " + localizationId);
            }
            return result;
        }
        #endregion
    }
}
