using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Tridion.Api.Client;
using Sdl.Tridion.Api.Client.ContentModel;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Tridion.ApiClient;
using Sdl.Web.Tridion.Linking;
using Sdl.Web.Tridion.Navigation;
using Sdl.Web.Tridion.Caching;
using Sdl.Web.Tridion.Mapping;
using Sdl.Web.Tridion.Providers.Binary;

namespace Sdl.Web.Tridion.Tests
{
    internal class TestFixture : ILocalizationResolver
    {
        internal static readonly string HomePageId = "277"; // /autotest-parent homepage Id
        internal const string NavigationTaxonomyTitle = "Test Taxonomy [Navigation]";
        internal const string TopLevelKeyword1Title = "Top-level Keyword 1";
        internal const string TopLevelKeyword2Title = "Top-level Keyword 2";
        internal const string Keyword1_1Title = "Keyword 1.1";
        internal const string Keyword1_2Title = "Keyword 1.2";

        internal const string ArticlePageRelativeUrlPath = "test_article_page";
        internal const string ArticleDynamicPageRelativeUrlPath = "test_article_dynamic";
        internal const string ComponentLinkTestPageRelativeUrlPath = "comp_link_test_page";
        internal const string ComponentLinkTest2PageRelativeUrlPath = "comp_link_test_page_2";
        internal const string ComponentLinkTest2BPageRelativeUrlPath = "regression/comp_link_test_page_2";
        internal const string MediaManagerTestPageRelativeUrlPath = "mm_test.html";
        internal const string SmartTargetTestPageRelativeUrlPath = "smoke/smart-target-smoke-test";
        internal const string ContextExpressionsTestPageRelativeUrlPath = "smoke/context-expression-smoke-test";
        internal const string TaxonomyTestPage1RelativeUrlPath = "regression/taxonomy/nav-taxonomy-test-1.html";
        internal const string TaxonomyTestPage2RelativeUrlPath = "regression/taxonomy/nav-taxonomy-test-2.html";
        internal const string TaxonomyIndexPageRelativeUrlPath = "regression/taxonomy";
        internal const string Tsi811PageRelativeUrlPath = "regression/tsi-811";
        internal const string Tsi1278PageRelativeUrlPath = "tsi-1278_trådløst.html";
        internal const string Tsi1308PageRelativeUrlPath = "regression/tsi-1308";
        internal const string Tsi1757PageRelativeUrlPath = "regression/tsi-1757";
        internal const string Tsi1614PageRelativeUrlPath = "tsi-1614.html";
        internal const string Tsi1758PageRelativeUrlPath = "regression/tsi-1758.html";
        internal const string Tsi1852PageRelativeUrlPath = "regression/tsi-1852.html";
        internal const string Tsi1946PageRelativeUrlPath = "regression/tsi-1946.html";
        internal const string Tsi2225PageRelativeUrlPath = "regression/tsi-2225";
        internal const string Tsi2277Page1RelativeUrlPath = "regression/tsi-2277-1";
        internal const string Tsi2277Page2RelativeUrlPath = "regression/tsi-2277-2";
        internal const string Tsi2285PageRelativeUrlPath = "regression/tsi-2285";
        internal const string Tsi2287PageRelativeUrlPath = "system/include/header";
        internal const string Tsi2316PageRelativeUrlPath = "regression/tsi-2316";
        internal const string Tsi2844PageRelativeUrlPath = "regression/tsi-2844";
        internal const string Tsi2844Page2RelativeUrlPath = "regression/tsi-2844/tsi-2844-page-metadata";
        internal const string Tsi3010PageRelativeUrlPath = "regression/tsi-3010";

        private static readonly IEnumerable<Localization> _testLocalizations;
        private static readonly Localization _parentLocalization;
        private static readonly Localization _childLocalization;
        private static readonly Localization _legacyParentLocalization;
        private static readonly Localization _legacyChildLocalization;

        private static readonly IDictionary<Type, object> _testProviders = new Dictionary<Type, object>
        {
            { typeof(ICacheProvider), new DefaultCacheProvider() },
            { typeof(IContentProvider), new GraphQLContentProvider() },
            { typeof(INavigationProvider), new StaticNavigationProvider() },
            { typeof(ILinkResolver), new GraphQLLinkResolver() },
            { typeof(IMediaHelper), new MockMediaHelper() },
            { typeof(ILocalizationResolver), new TestFixture() },
            { typeof(IContextClaimsProvider), new TestContextClaimsProvider() },
            { typeof(IConditionalEntityEvaluator), new MockConditionalEntityEvaluator() },
            { typeof(IBinaryProvider), new GraphQLBinaryProvider() }
        };

        static TestFixture()
        {
            /* dxadevwev85.ams.dev 
            _parentLocalization = new Localization
            {
                Id = "1065",
                Path = "/autotest-parent"
            };

            _childLocalization = new Localization
            {
                Id = "1078",
                Path = "/autotest-child"
            };

            _legacyParentLocalization = new Localization
            {
                Id = "1081",
                Path = "/autotest-parent-legacy"
            };

            _legacyChildLocalization = new Localization
            {
                Id = "1083",
                Path = "/autotest-child-legacy"
            }; */

            _parentLocalization = new Localization
            {
                Path = "/autotest-parent"
            };

            _childLocalization = new Localization
            {
                Path = "/autotest-child"
            };

            _legacyParentLocalization = new Localization
            {
                Path = "/autotest-parent-legacy"
            };

            _legacyChildLocalization = new Localization
            {
                Path = "/autotest-child-legacy"
            };

            _testLocalizations = new[]
            {
                _parentLocalization, _childLocalization, _legacyParentLocalization,
                _legacyChildLocalization
            };

            // map path of publications to Ids
            var client = ApiClientFactory.Instance.CreateClient();
            var publications = client.GetPublications(ContentNamespace.Sites, null, null, null, null);
            Assert.AreNotEqual(0, publications.Edges.Count,
                "No publications returned from content service. Check you have published all the relevant publications.");
            var publicationsLut = new Dictionary<string, string>();
            foreach (var x in publications.Edges.Where(x => !publicationsLut.ContainsKey(x.Node.PublicationUrl)))
            {
                publicationsLut.Add(x.Node.PublicationUrl, x.Node.PublicationId.ToString());
            }
            foreach (var x in _testLocalizations)
            {
                if (!publicationsLut.ContainsKey(x.Path)) continue;
                x.Id = publicationsLut[x.Path];
                if (x.Path != "/autotest-parent") continue;
                // Grab homepage id since this is used in some unit tests
                int pubId = int.Parse(x.Id);
                string path = $"{x.Path}/index.html";
                var page = client.GetPage(
                    ContentNamespace.Sites, pubId, path, null,
                    ContentIncludeMode.Exclude, null);
                if (page == null)
                    Assert.Fail("Unable to find /autotest-parent homepage Id");
                HomePageId = page.ItemId.ToString();
            }
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

        internal static Localization LegacyParentLocalization
        {
            get
            {
                _legacyParentLocalization.EnsureInitialized();
                return _legacyParentLocalization;
            }
        }

        internal static Localization LegacyChildLocalization
        {
            get
            {
                _legacyChildLocalization.EnsureInitialized();

                // Trick to allow us to test on a "Live" (not XPM-enabled) configuration even though we're actually on a Staging CD Environment:
                _legacyChildLocalization.IsXpmEnabled = false;
                return _legacyChildLocalization;
            }
        }

        internal static void InitializeProviders(Type modelServiceProviderType)
        {
            IModelServiceProvider modelServiceProvider = 
                (IModelServiceProvider) Activator.CreateInstance(modelServiceProviderType);
            modelServiceProvider.AddDataModelExtension(new DefaultModelBuilder());

            SiteConfiguration.InitializeProviders(interfaceType =>
            {
                object provider;
                if (interfaceType == typeof(IModelServiceProvider))
                    return modelServiceProvider;
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
            Localization result = _testLocalizations.FirstOrDefault(loc => loc.Id == localizationId);
            if (result == null)
            {
                throw new DxaUnknownLocalizationException("Unknown Localization ID: " + localizationId);
            }
            return result;
        }
        #endregion
    }
}
