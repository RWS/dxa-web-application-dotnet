using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using Sdl.Web.Tridion.Tests.Models;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public abstract class ContentProviderTest : TestClass
    {
        private readonly Func<Localization> _testLocalizationInitializer;
        private Localization _testLocalization;

        protected IContentProvider TestContentProvider { get; }

        protected Localization TestLocalization
        {
            get
            {
                if (_testLocalization == null)
                {
                    _testLocalization = _testLocalizationInitializer();
                }
                return _testLocalization;
            }
        }

        protected ContentProviderTest(IContentProvider contentProvider, Func<Localization> testLocalizationInitializer)
        {
            TestContentProvider = contentProvider;
            _testLocalizationInitializer = testLocalizationInitializer;
        }

        [TestMethod]
        public void GetPageModel_ImplicitIndexPage_Success()
        {
            string testPageUrlPath =  TestLocalization.Path; // Implicitly address the home page (index.html)
            string testPageUrlPath2 = TestLocalization.Path.Substring(1); // URL path not starting with slash
            string testPageUrlPath3 = TestLocalization.Path + "/"; // URL path ending with slash

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: false);
            PageModel pageModel2 = TestContentProvider.GetPageModel(testPageUrlPath2, TestLocalization, addIncludes: false);
            PageModel pageModel3 = TestContentProvider.GetPageModel(testPageUrlPath3, TestLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            Assert.IsNotNull(pageModel, "pageModel2");
            Assert.IsNotNull(pageModel, "pageModel3");

            OutputJson(pageModel);

            Assert.AreEqual(TestFixture.HomePageId, pageModel.Id, "pageModel.Id");
            Assert.AreEqual(TestFixture.HomePageId, pageModel2.Id, "pageModel2.Id");
            Assert.AreEqual(TestFixture.HomePageId, pageModel3.Id, "pageModel3.Id");
            Assert.AreEqual(TestLocalization.Path + Constants.IndexPageUrlSuffix, pageModel.Url, "Url");
            Assert.AreEqual(TestLocalization.Path + Constants.IndexPageUrlSuffix, pageModel2.Url, "pageModel2.Url");
            Assert.AreEqual(TestLocalization.Path + Constants.IndexPageUrlSuffix, pageModel3.Url, "pageModel3.Url");
        }

        [TestMethod]
        public void GetPageModel_NullUrlPath_Exception()
        {
            // null URL path is allowed, but it resolves to "/index" which does not exist in TestFixture.ParentLocalization.
            AssertThrowsException<DxaItemNotFoundException>(() => TestContentProvider.GetPageModel(null, TestFixture.ParentLocalization, addIncludes: false));
        }

        [TestMethod]
        public void GetPageModel_WithIncludes_Success()
        {
            string testPageUrlPath = TestLocalization.GetAbsoluteUrlPath(TestFixture.ArticlePageRelativeUrlPath);

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: true);

            Assert.IsNotNull(pageModel, "pageModel");
            OutputJson(pageModel);

            Assert.AreEqual(4, pageModel.Regions.Count, "pageModel.Regions.Count");
            RegionModel headerRegion = pageModel.Regions["Header"];
            Assert.IsNotNull(headerRegion, "headerRegion");
            Assert.IsNotNull(headerRegion.XpmMetadata, "headerRegion.XpmMetadata");
            Assert.AreEqual("Header", headerRegion.XpmMetadata[RegionModel.IncludedFromPageTitleXpmMetadataKey], "headerRegion.XpmMetadata[RegionModel.IncludedFromPageTitleXpmMetadataKey]");
            Assert.AreEqual("header", headerRegion.XpmMetadata[RegionModel.IncludedFromPageFileNameXpmMetadataKey], "headerRegion.XpmMetadata[RegionModel.IncludedFromPageFileNameXpmMetadataKey]");
        }

        [TestMethod]
        public void GetPageModel_InternationalizedUrl_Success() // See TSI-1278
        {
            string testPageUrlPath = TestLocalization.GetAbsoluteUrlPath(TestFixture.Tsi1278PageRelativeUrlPath);

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            OutputJson(pageModel);

            MediaItem testImage = pageModel.Regions["Main"].Entities[0] as MediaItem;
            Assert.IsNotNull(testImage, "testImage");
            StringAssert.Contains(testImage.Url, "tr%C3%A5dl%C3%B8st", "testImage.Url");
        }

        [TestMethod]
        public void GetPageModel_EclItem_Success()
        {
            string testPageUrlPath = TestLocalization.GetAbsoluteUrlPath(TestFixture.MediaManagerTestPageRelativeUrlPath);

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            OutputJson(pageModel);

            MediaManagerDistribution mmDistribution = pageModel.Regions["Main"].Entities[0] as MediaManagerDistribution;
            Assert.IsNotNull(mmDistribution, "mmDistribution");
            Assert.IsNotNull(mmDistribution.EclUri, "mmDistribution.EclUri");
            StringAssert.Matches(mmDistribution.EclUri, new Regex(@"ecl:\d+-mm-.*"), "mmDistribution.EclUri");
            Assert.AreEqual("imagedist", mmDistribution.EclDisplayTypeId, "mmDistribution.EclDisplayTypeId");
            Assert.IsNotNull(mmDistribution.EclTemplateFragment, "mmDistribution.EclTemplateFragment");
            Assert.IsNotNull(mmDistribution.EclExternalMetadata, "mmDistribution.EclExternalMetadata");
            Assert.IsTrue(mmDistribution.EclExternalMetadata.Keys.Count >= 11, "mmDistribution.EclExternalMetadata.Keys.Count");
            Assert.AreEqual("Image", mmDistribution.EclExternalMetadata["OutletType"], "mmDistribution.EclExternalMetadata['OutletType']");
        }

        [TestMethod]
        public void GetPageModel_Meta_Success() // See TSI-1308
        {
            string testPageUrlPath = TestLocalization.GetAbsoluteUrlPath(TestFixture.Tsi1308PageRelativeUrlPath);

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            OutputJson(pageModel);

            IDictionary<string, string> pageMeta = pageModel.Meta;
            Assert.IsNotNull(pageMeta, "pageMeta");
            Assert.AreEqual(15, pageMeta.Count, "pageMeta.Count");
            Assert.AreEqual("This is single line text", pageMeta["singleLineText"], "pageMeta[singleLineText]");
            Assert.AreEqual("This is multi line text line 1\nAnd line 2\n", pageMeta["multiLineText"], "pageMeta[multiLineText]");
            Assert.AreEqual($"This is <strong>rich</strong> text with a <a title=\"Test Article\" href=\"{TestLocalization.Path}/test_article_dynamic\">Component Link</a>", pageMeta["richText"], "pageMeta[richText]");
            Assert.AreEqual("News Article", pageMeta["keyword"], "pageMeta[keyword]");
            Assert.AreEqual($"{TestLocalization.Path}/test_article_dynamic", pageMeta["componentLink"], "pageMeta[componentLink]");
            Assert.AreEqual($"{TestLocalization.Path}/Images/company-news-placeholder_tcm{TestLocalization.Id}-4480.png", pageMeta["mmComponentLink"], "pageMeta[mmComponentLink]");
            StringAssert.StartsWith(pageMeta["date"], "1970-12-16T12:34:56", "pageMeta[date]");
            Assert.AreEqual("666.666", pageMeta["number"], "pageMeta[number]");
            Assert.AreEqual("Rick Pannekoek", pageMeta["author"], "pageMeta[author]");
            Assert.AreEqual("TSI-1308 Test Page", pageMeta["og:title"], "pageMeta[og: title]");
            Assert.AreEqual("TSI-1308 Test Page", pageMeta["description"], "pageMeta[description]");
        }


    }
}
