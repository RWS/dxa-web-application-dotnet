using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        public void GetPageModel_NonExistent_Exception()
        {
            AssertThrowsException<DxaItemNotFoundException>(() => TestContentProvider.GetPageModel("/does/not/exist", TestFixture.ParentLocalization));
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
           // TODO: Assert.IsNotNull(mmDistribution.EclTemplateFragment, "mmDistribution.EclTemplateFragment");
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

        [TestMethod]
        public void GetPageModel_XpmMetadataOnStaging_Success()
        {
            string testPageUrlPath =  TestLocalization.GetAbsoluteUrlPath(TestFixture.ArticlePageRelativeUrlPath);

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: true);

            Assert.IsNotNull(pageModel, "pageModel");
            OutputJson(pageModel);

            Assert.IsNotNull(pageModel.XpmMetadata, "pageModel.XpmMetadata");
            Assert.AreEqual(testPageUrlPath, pageModel.Url, "pageModel.Url");

            RegionModel headerRegion = pageModel.Regions["Header"];
            Assert.IsNotNull(headerRegion, "headerRegion");
            Assert.IsNotNull(headerRegion.XpmMetadata, "headerRegion.XpmMetadata");
            Assert.AreEqual("Header", headerRegion.XpmMetadata[RegionModel.IncludedFromPageTitleXpmMetadataKey], "headerRegion.XpmMetadata[RegionModel.IncludedFromPageTitleXpmMetadataKey]");
            Assert.AreEqual("header", headerRegion.XpmMetadata[RegionModel.IncludedFromPageFileNameXpmMetadataKey], "headerRegion.XpmMetadata[RegionModel.IncludedFromPageFileNameXpmMetadataKey]");

            RegionModel mainRegion = pageModel.Regions["Main"];
            Assert.IsNotNull(mainRegion, "mainRegion");
            Assert.IsNull(mainRegion.XpmMetadata, "mainRegion.XpmMetadata");

            Article testArticle = mainRegion.Entities[0] as Article;
            Assert.IsNotNull(testArticle, "Test Article not found on Page.");

            Assert.IsNotNull(testArticle.XpmMetadata, "testArticle.XpmMetadata");
            Assert.IsNotNull(testArticle.XpmPropertyMetadata, "testArticle.XpmPropertyMetadata");

            object isQueryBased;
            Assert.IsFalse(testArticle.XpmMetadata.TryGetValue("IsQueryBased", out isQueryBased), "XpmMetadata contains 'IsQueryBased'");
            object isRepositoryPublished;
            Assert.IsTrue(testArticle.XpmMetadata.TryGetValue("IsRepositoryPublished", out isRepositoryPublished), "XpmMetadata contains 'IsRepositoryPublished'");
            Assert.AreEqual(false, isRepositoryPublished, "IsRepositoryPublished value");

            // NOTE: boolean value must not have quotes in XPM markup (TSI-1251)
            string xpmMarkup = testArticle.GetXpmMarkup(TestLocalization);
            StringAssert.DoesNotMatch(xpmMarkup, new Regex("IsQueryBased"), "XPM markup");
            StringAssert.Contains(xpmMarkup, "\"IsRepositoryPublished\":false", "XPM markup");
        }

        [TestMethod]
        public void GetPageModel_DynamicComponentPresentation_Success()
        {
            string articlePageUrlPath = TestLocalization.GetAbsoluteUrlPath(TestFixture.ArticlePageRelativeUrlPath);
            string articleDynamicPageUrlPath = TestLocalization.GetAbsoluteUrlPath(TestFixture.ArticleDynamicPageRelativeUrlPath);

            PageModel referencePageModel = TestContentProvider.GetPageModel(articlePageUrlPath, TestLocalization, addIncludes: false);
            Assert.IsNotNull(referencePageModel, "referencePageModel");
            Article referenceArticle = referencePageModel.Regions["Main"].Entities[0] as Article;
            Assert.IsNotNull(referenceArticle, "testArticle");

            PageModel pageModel = TestContentProvider.GetPageModel(articleDynamicPageUrlPath, TestLocalization, addIncludes: false);
            Assert.IsNotNull(pageModel, "pageModel");
            OutputJson(pageModel);

            Article dcpArticle = pageModel.Regions["Main"].Entities[0] as Article;
            Assert.IsNotNull(dcpArticle, "dcpArticle");
            Assert.AreEqual(TestFixture.ArticleDcpEntityId, dcpArticle.Id, "dcpArticle.Id"); // EntityModel.Id for DCP is different
            Assert.AreEqual(referenceArticle.Headline, dcpArticle.Headline, "dcpArticle.Headline");
            AssertEqualCollections(referenceArticle.ArticleBody, dcpArticle.ArticleBody, "dcpArticle.ArticleBody");
            AssertEqualCollections(referenceArticle.XpmPropertyMetadata, dcpArticle.XpmPropertyMetadata, "dcpArticle.XpmPropertyMetadata");
            Assert.IsNotNull(dcpArticle.XpmMetadata, "dcpArticle.XpmMetadata");
            Assert.AreEqual(true, dcpArticle.XpmMetadata["IsRepositoryPublished"], "dcpArticle.XpmMetadata['IsRepositoryPublished']");
        }

        [TestMethod]
        public void GetPageModel_EmbeddedEntityModels_Success() // See TSI-1758
        {
            string testPageUrlPath = TestLocalization.GetAbsoluteUrlPath(TestFixture.Tsi1758PageRelativeUrlPath);

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            OutputJson(pageModel);

            Tsi1758TestEntity testEntity = pageModel.Regions["Main"].Entities[0] as Tsi1758TestEntity;
            Assert.IsNotNull(testEntity, "testEntity");
            Assert.IsNotNull(testEntity.EmbedField1, "testEntity.EmbedField1");
            Assert.IsNotNull(testEntity.EmbedField2, "testEntity.EmbedField2");
            Assert.AreEqual(2, testEntity.EmbedField1.Count, "testEntity.EmbedField1.Count");
            Assert.AreEqual(2, testEntity.EmbedField2.Count, "testEntity.EmbedField2.Count");
            Assert.AreEqual("This is the textField of the first embedField1", testEntity.EmbedField1[0].TextField, "testEntity.EmbedField1[0].TextField");
            Assert.AreEqual("This is the textField of the second embedField1", testEntity.EmbedField1[1].TextField, "testEntity.EmbedField1[1].TextField");
            Assert.AreEqual("This is the textField of the first embedField2", testEntity.EmbedField2[0].TextField, "testEntity.EmbedField2[0].TextField");
            Assert.AreEqual("This is the textField of the second embedField2", testEntity.EmbedField2[1].TextField, "testEntity.EmbedField2[1].TextField");

            Assert.IsNotNull(testEntity.EmbedField1[0].EmbedField1, "testEntity.EmbedField1[0].EmbedField1");
            Assert.IsNotNull(testEntity.EmbedField2[0].EmbedField1, "testEntity.EmbedField2[0].EmbedField1");
        }

        [TestMethod]
        public void GetPageModel_OptionalFieldsXpmMetadata_Success() // See TSI-1946
        {
            string testPageUrlPath = TestLocalization.GetAbsoluteUrlPath(TestFixture.Tsi1946PageRelativeUrlPath);

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            Article testArticle = pageModel.Regions["Main"].Entities.OfType<Article>().FirstOrDefault();
            Assert.IsNotNull(testArticle, "testArticle");
            Tsi1946TestEntity testEntity = pageModel.Regions["Main"].Entities.OfType<Tsi1946TestEntity>().FirstOrDefault();
            Assert.IsNotNull(testEntity, "testEntity");

            OutputJson(testArticle);
            OutputJson(testEntity);

            Assert.AreEqual(5, testArticle.XpmPropertyMetadata.Count, "testArticle.XpmPropertyMetadata.Count");
            Assert.AreEqual("tcm:Content/custom:Article/custom:image", testArticle.XpmPropertyMetadata["Image"], "testArticle.XpmPropertyMetadata[Image]");
            Assert.AreEqual("tcm:Metadata/custom:Metadata/custom:standardMeta/custom:description", testArticle.XpmPropertyMetadata["Description"], "testArticle.XpmPropertyMetadata[Description]");

            Paragraph testParagraph = testArticle.ArticleBody[0];
            Assert.AreEqual(4, testParagraph.XpmPropertyMetadata.Count, "testParagraph.XpmPropertyMetadata.Count");
            Assert.AreEqual("tcm:Content/custom:Article/custom:articleBody[1]/custom:caption", testParagraph.XpmPropertyMetadata["Caption"], "testParagraph.XpmPropertyMetadata[Caption]");

            Assert.AreEqual(7, testEntity.XpmPropertyMetadata.Count, "testEntity.XpmPropertyMetadata.Count");
        }

        [TestMethod]
        public void GetPageModel_KeywordMapping_Success() // See TSI-811
        {
            string testPageUrlPath = TestLocalization.GetAbsoluteUrlPath(TestFixture.Tsi811PageRelativeUrlPath);

            Tsi811PageModel pageModel = (Tsi811PageModel) TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            OutputJson(pageModel);

            Tsi811TestEntity testEntity = pageModel.Regions["Main"].Entities[0] as Tsi811TestEntity;
            Assert.IsNotNull(testEntity, "testEntity");
            Assert.IsNotNull(testEntity.Keyword1, "testEntity.Keyword1");
            Assert.IsNotNull(testEntity.Keyword2, "testEntity.Keyword2");
            Assert.IsTrue(testEntity.BooleanProperty, "testEntity.BooleanProperty");

            Assert.AreEqual(2, testEntity.Keyword1.Count, "testEntity.Keyword1.Count");
            AssertValidKeywordModel(testEntity.Keyword1[0], "Test Keyword 1", "TSI-811 Test Keyword 1", "Key 1", "testEntity.Keyword1[0]");
            AssertValidKeywordModel(testEntity.Keyword1[1], "Test Keyword 2", "TSI-811 Test Keyword 2", "Key 2", "testEntity.Keyword1[1]");
            AssertValidKeywordModel(testEntity.Keyword2, "News Article", string.Empty, "core.newsArticle", "testEntity.Keyword2");

            Tsi811TestKeyword testKeyword1 = testEntity.Keyword1[0];
            Assert.AreEqual("This is Test Keyword 1's textField", testKeyword1.TextField, "testKeyword1.TextField");
            Assert.AreEqual(666.66, testKeyword1.NumberProperty, "testKeyword1.NumberProperty");

            Tsi811TestKeyword pageKeyword = pageModel.PageKeyword;
            AssertValidKeywordModel(pageKeyword, "Test Keyword 2", "TSI-811 Test Keyword 2", "Key 2", "pageKeyword");
            Assert.AreEqual("This is textField of Test Keyword 2", pageKeyword.TextField, "pageKeyword.TextField");
            Assert.AreEqual(999.99, pageKeyword.NumberProperty, "pageKeyword.NumberProperty");
        }

        private static void AssertValidKeywordModel(KeywordModel keywordModel, string expectedTitle, string expectedDescription, string expectedKey, string subjectName)
        {
            Assert.IsNotNull(keywordModel, subjectName);
            Assert.AreEqual(expectedTitle, keywordModel.Title, subjectName + ".Title");
            Assert.AreEqual(expectedDescription, keywordModel.Description, subjectName + ".Description");
            Assert.AreEqual(expectedKey, keywordModel.Key, subjectName + ".Key");
            StringAssert.Matches(keywordModel.Id, new Regex(@"\d+"), subjectName + ".Id");
            StringAssert.Matches(keywordModel.TaxonomyId, new Regex(@"\d+"), subjectName + ".TaxonomyId");
        }

        [TestMethod]
        public void GetPageModel_ConditionalEntities_Success()
        {
            string testPageUrlPath = TestLocalization.GetAbsoluteUrlPath(TestFixture.ArticlePageRelativeUrlPath);

            // Verify pre-test state: Test Page should contain 1 Article
            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: false);
            Assert.IsNotNull(pageModel, "pageModel");
            Assert.AreEqual(1, pageModel.Regions["Main"].Entities.Count, "pageModel.Regions[Main].Entities.Count");
            Article testArticle = (Article) pageModel.Regions["Main"].Entities[0];

            try
            {
                MockConditionalEntityEvaluator.EvaluatedEntities.Clear();
                MockConditionalEntityEvaluator.ExcludeEntityIds.Add(testArticle.Id);

                // Test Page's Article should now be suppressed by MockConditionalEntityEvaluator
                PageModel pageModel2 = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: false);
                Assert.IsNotNull(pageModel2, "pageModel2");
                Assert.AreEqual(0, pageModel2.Regions["Main"].Entities.Count, "pageModel2.Regions[Main].Entities.Count");
                Assert.AreEqual(1, MockConditionalEntityEvaluator.EvaluatedEntities.Count, "MockConditionalEntityEvaluator.EvaluatedEntities.Count");
            }
            finally
            {
                MockConditionalEntityEvaluator.ExcludeEntityIds.Clear();
            }

            // Verify post-test state: Test Page should still contain 1 Article
            PageModel pageModel3 = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: false);
            Assert.IsNotNull(pageModel3, "pageModel3");
            Assert.AreEqual(1, pageModel3.Regions["Main"].Entities.Count, "pageModel3.Regions[Main].Entities.Count");
        }

        [TestMethod]
        public void GetPageModel_RichTextProcessing_Success()
        {
            string testPageUrlPath = TestLocalization.GetAbsoluteUrlPath(TestFixture.ArticlePageRelativeUrlPath);

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            Assert.AreEqual(testPageUrlPath, pageModel.Url, "pageModel.Url");

            Article testArticle = pageModel.Regions["Main"].Entities[0] as Article;
            Assert.IsNotNull(testArticle, "testArticle");
            OutputJson(testArticle);

            RichText content = testArticle.ArticleBody[0].Content;
            Assert.IsNotNull(content, "content");
            Assert.AreEqual(3, content.Fragments.Count(), "content.Fragments.Count");

            Image image = content.Fragments.OfType<Image>().FirstOrDefault();
            Assert.IsNotNull(image, "image");
            Assert.IsTrue(image.IsEmbedded, "image.IsEmbedded");
            Assert.IsNotNull(image.MvcData, "image.MvcData");
            Assert.AreEqual("Image", image.MvcData.ViewName, "image.MvcData.ViewName");

            string firstHtmlFragment = content.Fragments.First().ToHtml();
            Assert.IsNotNull(firstHtmlFragment, "firstHtmlFragment");
            string linkPattern1 = string.Format(@"Component link \(published\): <a title=""TSI-1758 Test Component"" href=""{0}/regression/tsi-1758"">TSI-1758 Test Component</a>", TestLocalization.Path);
            string linkPattern2 = string.Format(@"MMC link: <a title=""bulls-eye"" href=""{0}/Images/bulls-eye.*"">bulls-eye</a>", TestLocalization.Path);
            StringAssert.Matches(firstHtmlFragment, new Regex(@"Component link \(not published\): Test Component"));
            StringAssert.Matches(firstHtmlFragment, new Regex(linkPattern1));
            StringAssert.Matches(firstHtmlFragment, new Regex(linkPattern2));
        }

        [TestMethod]
        public void GetPageModel_RichTextImageWithHtmlClass_Success() // See TSI-1614
        {
            string testPageUrlPath = TestLocalization.GetAbsoluteUrlPath(TestFixture.Tsi1614PageRelativeUrlPath);

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            Article testArticle = pageModel.Regions["Main"].Entities[0] as Article;
            Assert.IsNotNull(testArticle, "Test Article not found on Page.");
            Image testImage = testArticle.ArticleBody[0].Content.Fragments.OfType<Image>().FirstOrDefault();
            Assert.IsNotNull(testImage, "Test Image not found in Rich Text");
            Assert.AreEqual("test tsi1614", testImage.HtmlClasses, "Image.HtmlClasses");
        }

        [TestMethod]
        [Ignore] // TODO TSI-2266
        public void GetPageModel_LanguageSelector_Success() // See TSI-2225
        {
            string testPageUrlPath = TestLocalization.GetAbsoluteUrlPath(TestFixture.Tsi2225PageRelativeUrlPath);

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            OutputJson(pageModel);

            Common.Models.Configuration configEntity = pageModel.Regions["Nav"].Entities[0] as Common.Models.Configuration;
            Assert.IsNotNull(configEntity, "configEntity");
            Assert.AreEqual("tcm:1065-9712", configEntity.Settings["defaultContentLink"], "configEntity.Settings['defaultContentLink']");
            Assert.AreEqual("pt,mx", configEntity.Settings["suppressLocalizations"], "configEntity.Settings['suppressLocalizations']");
        }

        [TestMethod]
        public void GetPageModel_SmartTarget_Success()
        {
            string testPageUrlPath = TestLocalization.GetAbsoluteUrlPath(TestFixture.SmartTargetTestPageRelativeUrlPath);

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            OutputJson(pageModel);

            SmartTargetRegion example1Region = (SmartTargetRegion) pageModel.Regions["Example1"];
            Assert.IsNotNull(example1Region, "example1Region");

            SmartTargetRegion example2Region = (SmartTargetRegion) pageModel.Regions["Example2"];
            Assert.IsNotNull(example2Region, "example2Region");
        }

        [TestMethod]
        public void GetEntityModel_NonExistent_Exception()
        {
            AssertThrowsException<DxaItemNotFoundException>(() => TestContentProvider.GetEntityModel("666-666", TestLocalization));
        }

        [TestMethod]
        public void GetEntityModel_InvalidId_Exception()
        {
            AssertThrowsException<DxaException>(() => TestContentProvider.GetEntityModel("666", TestLocalization));
        }

        [TestMethod]
        public void GetEntityModel_XpmMetadataOnStaging_Success()
        {
            const string testEntityId = TestFixture.ArticleDcpEntityId;

            EntityModel entityModel = TestContentProvider.GetEntityModel(testEntityId, TestLocalization);

            Assert.IsNotNull(entityModel, "entityModel");
            OutputJson(entityModel);

            Assert.AreEqual(testEntityId, entityModel.Id, "entityModel.Id");
            Assert.IsNotNull(entityModel.XpmMetadata, "entityModel.XpmMetadata");
            Assert.IsNotNull(entityModel.XpmPropertyMetadata, "entityModel.XpmPropertyMetadata");

            object isQueryBased;
            Assert.IsTrue(entityModel.XpmMetadata.TryGetValue("IsQueryBased", out isQueryBased), "XpmMetadata contains 'IsQueryBased'");
            Assert.AreEqual(true, isQueryBased, "IsQueryBased value");
            object isRepositoryPublished;
            Assert.IsTrue(entityModel.XpmMetadata.TryGetValue("IsRepositoryPublished", out isRepositoryPublished), "XpmMetadata contains 'IsRepositoryPublished'");
            Assert.AreEqual(true, isRepositoryPublished, "IsRepositoryPublished value");

            // NOTE: boolean value must not have quotes in XPM markup (TSI-1251)
            string xpmMarkup = entityModel.GetXpmMarkup(TestFixture.ParentLocalization);
            StringAssert.Contains(xpmMarkup, "\"IsQueryBased\":true", "XPM markup");
            StringAssert.Contains(xpmMarkup, "\"IsRepositoryPublished\":true", "XPM markup");
        }

        [TestMethod]
        public void GetStaticContentItem_NonExistent_Exception()
        {
            const string testStaticContentItemUrlPath = "/does/not/exist";
            AssertThrowsException<DxaItemNotFoundException>(() => TestContentProvider.GetStaticContentItem(testStaticContentItemUrlPath, TestLocalization));
        }

        [TestMethod]
        public void GetStaticContentItem_InternationalizedUrl_Success() // See TSI-1278
        {
            string testStaticContentItemUrlPath = TestLocalization.GetAbsoluteUrlPath(
                string.Format(TestFixture.Tsi1278StaticContentItemRelativeUrlPath, TestLocalization.Id)
                );

            using (StaticContentItem testStaticContentItem = TestContentProvider.GetStaticContentItem(testStaticContentItemUrlPath, TestLocalization))
            {
                Assert.IsNotNull(testStaticContentItem, "testStaticContentItem");
                Assert.AreEqual("image/jpeg", testStaticContentItem.ContentType, "testStaticContentItem.ContentType");
                Stream contentStream = testStaticContentItem.GetContentStream();
                Assert.IsNotNull(contentStream,"contentStream");
                Assert.AreEqual(192129, contentStream.Length, "contentStream.Length");
            }
        }

        [TestMethod]
        public void PopulateDynamicList_TeaserFallbackToDescription_Success() // See TSI-1852
        {
            string testPageUrlPath = TestLocalization.GetAbsoluteUrlPath(TestFixture.Tsi1852PageRelativeUrlPath);

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            ContentList<Teaser> testContentList = pageModel.Regions["Main"].Entities[0] as ContentList<Teaser>;
            Assert.IsNotNull(testContentList, "testContentList");
            Assert.IsNotNull(testContentList.ItemListElements, "testContentList.ItemListElements");
            Assert.AreEqual(0, testContentList.ItemListElements.Count, "testContentList.ItemListElements is not empty before PopulateDynamicList");

            TestContentProvider.PopulateDynamicList(testContentList, TestLocalization);
            OutputJson(testContentList);

            Teaser testTeaser = testContentList.ItemListElements.FirstOrDefault(t => t.Headline == "TSI-1852 Article");
            Assert.IsNotNull(testTeaser, "testTeaser");
            Assert.IsNotNull(testTeaser.Text, "testTeaser.Text");
            StringAssert.StartsWith(testTeaser.Text.ToString(), "This is the standard metadata description", "testTeaser.Text");
        }
    }
}
