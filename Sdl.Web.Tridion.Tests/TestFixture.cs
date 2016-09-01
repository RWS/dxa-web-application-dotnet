using System;
using System.Collections.Generic;
using System.Linq;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Tridion.Linking;
using Sdl.Web.Tridion.Mapping;
using Sdl.Web.Tridion.Navigation;

namespace Sdl.Web.Tridion.Tests
{
    internal class TestFixture : ILocalizationResolver
    {
        internal const string HomePageId = "640";
        internal const string ArticleDcpEntityId = "9712-9711";
        internal const string ArticlePageUrlPath = "/autotest-parent/test_article_page.html";
        internal const string Tsi1278PageUrlPath = "/autotest-parent/tsi-1278_trådløst.html";
        internal const string Tsi1278StaticContentItemUrlPath = "/autotest-parent/Images/trådløst_tcm1065-9791.jpg";
        internal const string Tsi1614PageUrlPath = "/autotest-parent/tsi-1614.html";
        internal const string Tsi1758PageUrlPath = "/autotest-parent/regression/tsi-1758.html";
        internal const string Tsi1852PageUrlPath = "/autotest-parent/regression/tsi-1852.html";
        internal const string TaxonomyTestPage1UrlPath = "/autotest-parent/regression/taxonomy/nav-taxonomy-test-1.html";
        internal const string TaxonomyTestPage2UrlPath = "/autotest-parent/regression/taxonomy/nav-taxonomy-test-2.html";
        internal const string TaxonomyIndexPageUrlPath = "/autotest-parent/regression/taxonomy";
        internal const string NavigationTaxonomyTitle = "Test Taxonomy [Navigation]";
        internal const string TopLevelKeyword1Title = "Top-level Keyword 1";
        internal const string TopLevelKeyword2Title = "Top-level Keyword 2";
        internal const string Keyword1_1Title = "Keyword 1.1";

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
            { typeof(IMediaHelper), new TestMediaHelper() },
            { typeof(ILocalizationResolver), new TestFixture() },
            { typeof(IContextClaimsProvider), new TestContextClaimsProvider() }
        };

        static TestFixture()
        {
            // TODO: Retrieve Localization Info from CM (?)

            _parentLocalization = new Localization
            {
                LocalizationId = "1065",
                Path = "/autotest-parent"
            };

            _childLocalization = new Localization
            {
                LocalizationId = "1066",
                Path = "/autotest-child"
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
