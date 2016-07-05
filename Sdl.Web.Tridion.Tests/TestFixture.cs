using System;
using System.Collections.Generic;
using System.Linq;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Tridion.Linking;
using Sdl.Web.Tridion.Mapping;

namespace Sdl.Web.Tridion.Tests
{
    internal class TestFixture : ILocalizationResolver
    {
        private static readonly IEnumerable<Localization> _testLocalizations;
        private static readonly Localization _parentLocalization;
        private static readonly Localization _childLocalization;

        private static readonly IDictionary<Type, object> _testProviders = new Dictionary<Type, object>
        {
            { typeof(IContentProvider), new DefaultProvider() },
            { typeof(INavigationProvider), new DefaultProvider() },
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

            HomePageId = "640";
            ArticleDcpEntityId = "9712-9711";
            ArticlePageUrlPath = "/autotest-parent/test_article_page.html";
            Tsi1278PageUrlPath = "/autotest-parent/tsi-1278_trådløst.html";
            Tsi1278StaticContentItemUrlPath = "/autotest-parent/Images/trådløst_tcm1065-9791.jpg";
            Tsi1614PageUrlPath = "/autotest-parent/tsi-1614.html";

            TestRegistration.RegisterCoreViewModels();
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

        internal static string ArticleDcpEntityId { get; private set; }
        internal static string HomePageId { get; private set; }
        internal static string ArticlePageUrlPath { get; private set; }
        internal static string Tsi1278PageUrlPath { get; private set; }
        internal static string Tsi1278StaticContentItemUrlPath { get; private set; }
        internal static string Tsi1614PageUrlPath { get; private set; }


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
