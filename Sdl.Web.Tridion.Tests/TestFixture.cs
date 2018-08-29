using System;
using System.Collections.Generic;
using System.Linq;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Navigation;
using Sdl.Web.Tridion.Linking;
using Sdl.Web.Tridion.Navigation;
using Sdl.Web.Tridion.Caching;
using Sdl.Web.Tridion.Mapping;
using Sdl.Web.Tridion.ModelService;
using Sdl.Web.Tridion.Providers.Binary;

namespace Sdl.Web.Tridion.Tests
{
    internal class TestDataModelExtensions : IDataModelExtension
    {
        public Type ResolveDataModelType(string assemblyName, string typeName)
        {
            // perform type remapping as the type names returned from model-service do not match so we
            // remap here to correctly deserialize.
            switch (typeName)
            {
                case "TaxonomyNodeModelData":
                    return typeof(TaxonomyNode);
                case "SitemapItemModelData":
                    return typeof(SitemapItem);
            }
            // check for any extensions
            return Type.GetType($"Sdl.Web.DataModel.Extension.{typeName}");
        }
    }

    internal class TestFixture : ILocalizationResolver
    {
        //internal const string HomePageId = "640"; // dxadevweb85.ams.dev
        internal const string HomePageId = "279";
        //internal const string ArticleDcpEntityId = "9712-9711";
        internal const string ArticleDcpEntityId = "462-644";
        internal const string NavigationTaxonomyTitle = "Test Taxonomy [Navigation]";
        internal const string TopLevelKeyword1Title = "Top-level Keyword 1";
        internal const string TopLevelKeyword2Title = "Top-level Keyword 2";
        internal const string Keyword1_1Title = "Keyword 1.1";
        internal const string Keyword1_2Title = "Keyword 1.2";

        internal const string ArticlePageRelativeUrlPath = "test_article_page";
        internal const string ArticleDynamicPageRelativeUrlPath = "test_article_dynamic";
        internal const string ComponentLinkTestPageRelativeUrlPath = "comp_link_test_page";
        internal const string MediaManagerTestPageRelativeUrlPath = "mm_test.html";
        internal const string SmartTargetTestPageRelativeUrlPath = "smoke/smart-target-smoke-test";
        internal const string ContextExpressionsTestPageRelativeUrlPath = "smoke/context-expression-smoke-test";
        internal const string TaxonomyTestPage1RelativeUrlPath = "regression/taxonomy/nav-taxonomy-test-1.html";
        internal const string TaxonomyTestPage2RelativeUrlPath = "regression/taxonomy/nav-taxonomy-test-2.html";
        internal const string TaxonomyIndexPageRelativeUrlPath = "regression/taxonomy";
        internal const string Tsi811PageRelativeUrlPath = "regression/tsi-811";
        internal const string Tsi1278PageRelativeUrlPath = "tsi-1278_trådløst.html";
        //internal const string Tsi1278StaticContentItemRelativeUrlPath = "Images/trådløst_tcm{0}-9791.jpg";
        internal const string Tsi1278StaticContentItemRelativeUrlPath = "Images/trådløst_tcm{0}-510.jpg";
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

        private static readonly IEnumerable<ILocalization> _testLocalizations;
        private static readonly ILocalization _parentLocalization;
        private static readonly ILocalization _childLocalization;
        private static readonly ILocalization _legacyParentLocalization;
        private static readonly ILocalization _legacyChildLocalization;

        private static readonly IDictionary<Type, object> _testProviders = new Dictionary<Type, object>
        {
            { typeof(ICacheProvider), new DefaultCacheProvider() },
            { typeof(IModelServiceProvider), new GraphQLModelServiceProvider() },
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
            };*/


            /* http://cm.dev.dxa.sdldev.net */
            _parentLocalization = new Localization
            {
                Id = "6",
                Path = "/autotest-parent"
            };

            _childLocalization = new Localization
            {
                Id = "7",
                Path = "/autotest-child"
            };

            _legacyParentLocalization = new Localization
            {
                Id = "8",
                Path = "/autotest-parent-legacy"
            };

            _legacyChildLocalization = new Localization
            {
                Id = "9",
                Path = "/autotest-child-legacy"
            };

            _testLocalizations = new[] { _parentLocalization, _childLocalization, _legacyParentLocalization, _legacyChildLocalization };

            TestRegistration.RegisterViewModels();
        }

        internal static ILocalization ParentLocalization
        {
            get
            {
                _parentLocalization.EnsureInitialized();
                return _parentLocalization;
            }
        }

        internal static ILocalization ChildLocalization
        {
            get
            {
                _childLocalization.EnsureInitialized();
                return _childLocalization;
            }
        }

        internal static ILocalization LegacyParentLocalization
        {
            get
            {
                _legacyParentLocalization.EnsureInitialized();
                return _legacyParentLocalization;
            }
        }

        internal static ILocalization LegacyChildLocalization
        {
            get
            {
                _legacyChildLocalization.EnsureInitialized();

                // Trick to allow us to test on a "Live" (not XPM-enabled) configuration even though we're actually on a Staging CD Environment:
                _legacyChildLocalization.IsXpmEnabled = false;
                return _legacyChildLocalization;
            }
        }

        internal static void InitializeProviders()
        {
            object modelServiceProvider;
            if (_testProviders.TryGetValue(typeof(IModelServiceProvider), out modelServiceProvider))
            {
                ((IModelServiceProvider)modelServiceProvider).AddDataModelExtension(new TestDataModelExtensions());
            }

            SiteConfiguration.InitializeProviders(interfaceType =>
            {
                object provider;
                _testProviders.TryGetValue(interfaceType, out provider);
                return provider;
            });
        }

        #region ILocalizationResolver members
        public ILocalization ResolveLocalization(Uri url)
        {
            throw new NotImplementedException();
        }

        public ILocalization GetLocalization(string localizationId)
        {
            ILocalization result = _testLocalizations.FirstOrDefault(loc => loc.Id == localizationId);
            if (result == null)
            {
                throw new DxaUnknownLocalizationException("Unknown Localization ID: " + localizationId);
            }
            return result;
        }
        #endregion
    }
}
