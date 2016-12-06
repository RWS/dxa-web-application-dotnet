using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using Sdl.Web.Tridion.Mapping;
using Sdl.Web.Tridion.Tests.Models;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class ContentProviderTest : TestClass
    {
        private static readonly IContentProvider _testContentProvider = new DefaultContentProvider();

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }

        [TestMethod]
        public void GetPageModel_ImplicitIndexPage_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;
            string testPageUrlPath = testLocalization.Path; // Implicitly address the home page (index.html)
            string testPageUrlPath2 = testLocalization.Path.Substring(1); // URL path not starting with slash
            string testPageUrlPath3 = testLocalization.Path + "/"; // URL path ending with slash

            PageModel pageModel = _testContentProvider.GetPageModel(testPageUrlPath, testLocalization, addIncludes: false);
            PageModel pageModel2 = _testContentProvider.GetPageModel(testPageUrlPath2, testLocalization, addIncludes: false);
            PageModel pageModel3 = _testContentProvider.GetPageModel(testPageUrlPath3, testLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            Assert.IsNotNull(pageModel, "pageModel2");
            Assert.IsNotNull(pageModel, "pageModel3");
            Assert.AreEqual(TestFixture.HomePageId, pageModel.Id, "pageModel.Id");
            Assert.AreEqual(TestFixture.HomePageId, pageModel2.Id, "pageModel2.Id");
            Assert.AreEqual(TestFixture.HomePageId, pageModel3.Id, "pageModel3.Id");
            Assert.AreEqual(testLocalization.Path + Constants.IndexPageUrlSuffix, pageModel.Url, "Url");
            Assert.AreEqual(testLocalization.Path + Constants.IndexPageUrlSuffix, pageModel2.Url, "pageModel2.Url");
            Assert.AreEqual(testLocalization.Path + Constants.IndexPageUrlSuffix, pageModel3.Url, "pageModel3.Url");
        }

        [TestMethod]
        public void GetPageModel_NullUrlPath_Exception()
        {
            // null URL path is allowed, but it resolves to "/index" which does not exist in TestFixture.ParentLocalization.
            AssertThrowsException<DxaItemNotFoundException>(() => _testContentProvider.GetPageModel(null, TestFixture.ParentLocalization, addIncludes: false));
        }

        [TestMethod]
        public void GetPageModel_WithIncludes_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;
            string testPageUrlPath = TestFixture.ArticlePageUrlPath;

            PageModel pageModel = _testContentProvider.GetPageModel(testPageUrlPath, testLocalization, addIncludes: true);

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
        public void GetPageModel_EclItem_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;
            string testPageUrlPath = TestFixture.MediaManagerTestPageUrlPath;

            PageModel pageModel = _testContentProvider.GetPageModel(testPageUrlPath, testLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            OutputJson(pageModel);

            MediaManagerDistribution mmDistribution = pageModel.Regions["Main"].Entities[0] as MediaManagerDistribution;
            Assert.IsNotNull(mmDistribution, "mmDistribution");
            Assert.IsNotNull(mmDistribution.EclUri, "mmDistribution.EclUri");
            StringAssert.Matches(mmDistribution.EclUri, new Regex(@"ecl:\d+-mm-.*"), "mmDistribution.EclUri");
            Assert.AreEqual("imagedist", mmDistribution.EclDisplayTypeId, "mmDistribution.EclDisplayTypeId");
            Assert.IsNotNull(mmDistribution.EclTemplateFragment, "mmDistribution.EclTemplateFragment");
            Assert.IsNotNull(mmDistribution.EclExternalMetadata, "mmDistribution.EclExternalMetadata");
            Assert.AreEqual(11, mmDistribution.EclExternalMetadata.Keys.Count, "mmDistribution.EclExternalMetadata.Keys.Count");
            Assert.AreEqual("Image", mmDistribution.EclExternalMetadata["OutletType"], "mmDistribution.EclExternalMetadata['OutletType']");
        }


        [TestMethod]
        public void GetStaticContentItem_InternationalizedUrl_Success() // See TSI-1279 and TSI-1495
        {
            string testStaticContentItemUrlPath = TestFixture.Tsi1278StaticContentItemUrlPath;

            StaticContentItem staticContentItem = _testContentProvider.GetStaticContentItem(testStaticContentItemUrlPath, TestFixture.ParentLocalization);

            Assert.IsNotNull(staticContentItem, "staticContentItem");
        }

        [TestMethod]
        public void GetEntityModel_XpmMetadataOnStaging_Success()
        {
            const string testEntityId = TestFixture.ArticleDcpEntityId;

            EntityModel entityModel = _testContentProvider.GetEntityModel(testEntityId, TestFixture.ParentLocalization);

            Assert.IsNotNull(entityModel, "entityModel");
            Assert.AreEqual(testEntityId, entityModel.Id, "entityModel.Id");
            Assert.IsNotNull(entityModel.XpmMetadata, "entityModel.XpmMetadata");
            Assert.IsNotNull(entityModel.XpmPropertyMetadata, "entityModel.XpmPropertyMetadata");
            OutputJson(entityModel.XpmMetadata);
            OutputJson(entityModel.XpmPropertyMetadata);

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
        public void GetEntityModel_NoXpmMetadataOnLive_Success() // See TSI-1942
        {
            const string testEntityId = TestFixture.ArticleDcpEntityId;
            Localization testLocalization = TestFixture.ChildLocalization;

            EntityModel entityModel = _testContentProvider.GetEntityModel(testEntityId, testLocalization);

            Assert.IsNotNull(entityModel, "entityModel");
            Assert.AreEqual(testEntityId, entityModel.Id, "entityModel.Id");
            Assert.IsNull(entityModel.XpmMetadata, "entityModel.XpmMetadata");
            Assert.IsNull(entityModel.XpmPropertyMetadata, "entityModel.XpmPropertyMetadata");
            Assert.AreEqual(string.Empty, entityModel.GetXpmMarkup(testLocalization), "entityModel.GetXpmMarkup(testLocalization)");
        }

        [TestMethod]
        [ExpectedException(typeof(DxaItemNotFoundException))]
        public void GetPageModel_NonExistent_Exception()
        {
            _testContentProvider.GetPageModel("/does/not/exist", TestFixture.ParentLocalization);
        }

        [TestMethod]
        [ExpectedException(typeof(DxaItemNotFoundException))]
        public void GetEntityModel_NonExistent_Exception()
        {
            const string testEntityId = "666-666"; // Should not actually exist
            _testContentProvider.GetEntityModel(testEntityId, TestFixture.ParentLocalization);
        }

        [TestMethod]
        [ExpectedException(typeof(DxaException))]
        public void GetEntityModel_InvalidId_Exception()
        {
            _testContentProvider.GetEntityModel("666", TestFixture.ParentLocalization);
        }

        [TestMethod]
        public void GetPageModel_RichTextProcessing_Success()
        {
            string testPageUrlPath = TestFixture.ArticlePageUrlPath;

            PageModel pageModel = _testContentProvider.GetPageModel(testPageUrlPath, TestFixture.ParentLocalization, addIncludes: false);

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
            StringAssert.Matches(firstHtmlFragment, new Regex(@"Component link \(not published\): Test Component"));
            StringAssert.Matches(firstHtmlFragment, new Regex(@"Component link \(published\): <a title=""TSI-1758 Test Component"" href=""/autotest-parent/regression/tsi-1758"">TSI-1758 Test Component</a>"));
            StringAssert.Matches(firstHtmlFragment, new Regex(@"MMC link: <a title=""bulls-eye"" href=""/autotest-parent/Images/bulls-eye.*"">bulls-eye</a>"));
        }

        [TestMethod]
        public void GetPageModel_ConditionalEntities_Success()
        {
            string testPageUrlPath = TestFixture.ArticlePageUrlPath;
            Localization testLocalization = TestFixture.ParentLocalization;

            // Verify pre-test state: Test Page should contain 1 Article
            PageModel pageModel = _testContentProvider.GetPageModel(testPageUrlPath, testLocalization, addIncludes: false);
            Assert.IsNotNull(pageModel, "pageModel");
            Assert.AreEqual(1, pageModel.Regions["Main"].Entities.Count, "pageModel.Regions[Main].Entities.Count");
            Article testArticle = (Article) pageModel.Regions["Main"].Entities[0];

            try
            {
                MockConditionalEntityEvaluator.EvaluatedEntities.Clear();
                MockConditionalEntityEvaluator.ExcludeEntityIds.Add(testArticle.Id);

                // Test Page's Article should now be suppressed by MockConditionalEntityEvaluator
                PageModel pageModel2 = _testContentProvider.GetPageModel(testPageUrlPath, testLocalization, addIncludes: false);
                Assert.IsNotNull(pageModel2, "pageModel2");
                Assert.AreEqual(0, pageModel2.Regions["Main"].Entities.Count, "pageModel2.Regions[Main].Entities.Count");
                Assert.AreEqual(1, MockConditionalEntityEvaluator.EvaluatedEntities.Count, "MockConditionalEntityEvaluator.EvaluatedEntities.Count");
            }
            finally
            {
                MockConditionalEntityEvaluator.ExcludeEntityIds.Clear();
            }

            // Verify post-test state: Test Page should still contain 1 Article
            PageModel pageModel3 = _testContentProvider.GetPageModel(testPageUrlPath, testLocalization, addIncludes: false);
            Assert.IsNotNull(pageModel3, "pageModel3");
            Assert.AreEqual(1, pageModel3.Regions["Main"].Entities.Count, "pageModel3.Regions[Main].Entities.Count");
        }

        [TestMethod]
        public void GetPageModel_DynamicComponentPresentation_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;

            PageModel referencePageModel = _testContentProvider.GetPageModel(TestFixture.ArticlePageUrlPath, testLocalization, addIncludes: false);
            Assert.IsNotNull(referencePageModel, "referencePageModel");
            Article referenceArticle = referencePageModel.Regions["Main"].Entities[0] as Article;
            Assert.IsNotNull(referenceArticle, "testArticle");

            PageModel pageModel = _testContentProvider.GetPageModel(TestFixture.ArticleDynamicPageUrlPath, testLocalization, addIncludes: false);
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
        public void GetPageModel_XpmMetadataOnStaging_Success()
        {
            string testPageUrlPath = TestFixture.ArticlePageUrlPath;
            Localization testLocalization = TestFixture.ParentLocalization;

            PageModel pageModel = _testContentProvider.GetPageModel(testPageUrlPath, testLocalization, addIncludes: true);

            Assert.IsNotNull(pageModel, "pageModel");
            Assert.IsNotNull(pageModel.XpmMetadata, "pageModel.XpmMetadata");
            Assert.AreEqual(testPageUrlPath, pageModel.Url, "pageModel.Url");

            RegionModel headerRegion = pageModel.Regions["Header"];
            Assert.IsNotNull(headerRegion, "headerRegion");
            Assert.IsNotNull(headerRegion.XpmMetadata, "headerRegion.XpmMetadata");
            OutputJson(headerRegion.XpmMetadata);
            Assert.AreEqual("Header", headerRegion.XpmMetadata[RegionModel.IncludedFromPageTitleXpmMetadataKey], "headerRegion.XpmMetadata[RegionModel.IncludedFromPageTitleXpmMetadataKey]");
            Assert.AreEqual("header", headerRegion.XpmMetadata[RegionModel.IncludedFromPageFileNameXpmMetadataKey], "headerRegion.XpmMetadata[RegionModel.IncludedFromPageFileNameXpmMetadataKey]");

            RegionModel mainRegion = pageModel.Regions["Main"];
            Assert.IsNotNull(mainRegion, "mainRegion");
            Assert.IsNull(mainRegion.XpmMetadata, "mainRegion.XpmMetadata");

            Article testArticle = mainRegion.Entities[0] as Article;
            Assert.IsNotNull(testArticle, "Test Article not found on Page.");

            Assert.IsNotNull(testArticle.XpmMetadata, "testArticle.XpmMetadata");
            Assert.IsNotNull(testArticle.XpmPropertyMetadata, "testArticle.XpmPropertyMetadata");
            OutputJson(testArticle.XpmMetadata);
            OutputJson(testArticle.XpmPropertyMetadata);

            object isQueryBased;
            Assert.IsFalse(testArticle.XpmMetadata.TryGetValue("IsQueryBased", out isQueryBased), "XpmMetadata contains 'IsQueryBased'");
            object isRepositoryPublished;
            Assert.IsTrue(testArticle.XpmMetadata.TryGetValue("IsRepositoryPublished", out isRepositoryPublished), "XpmMetadata contains 'IsRepositoryPublished'");
            Assert.AreEqual(false, isRepositoryPublished, "IsRepositoryPublished value");

            // NOTE: boolean value must not have quotes in XPM markup (TSI-1251)
            string xpmMarkup = testArticle.GetXpmMarkup(testLocalization);
            StringAssert.DoesNotMatch(xpmMarkup, new Regex("IsQueryBased"), "XPM markup");
            StringAssert.Contains(xpmMarkup, "\"IsRepositoryPublished\":false", "XPM markup");
        }

        [TestMethod]
        public void GetPageModel_NoXpmMetadataOnLive_Success() // See TSI-1942
        {
            string testPageUrlPath = TestFixture.ArticleChildPageUrlPath;
            Localization testLocalization = TestFixture.ChildLocalization;

            PageModel pageModel = _testContentProvider.GetPageModel(testPageUrlPath, testLocalization, addIncludes: true);

            Assert.IsNotNull(pageModel, "pageModel");
            Assert.IsNull(pageModel.XpmMetadata, "pageModel.XpmMetadata");
            Assert.AreEqual(string.Empty, pageModel.GetXpmMarkup(testLocalization), "pageModel.GetXpmMarkup(testLocalization)");

            RegionModel headerRegion = pageModel.Regions["Header"];
            Assert.IsNotNull(headerRegion, "headerRegion");
            Assert.IsNull(headerRegion.XpmMetadata, "headerRegion.XpmMetadata");

            RegionModel mainRegion = pageModel.Regions["Main"];
            Assert.IsNotNull(mainRegion, "mainRegion");
            Assert.IsNull(mainRegion.XpmMetadata, "mainRegion.XpmMetadata");

            Article testArticle = mainRegion.Entities[0] as Article;
            Assert.IsNotNull(testArticle, "Test Article not found on Page.");
            Assert.IsNull(testArticle.XpmMetadata, "testArticle.XpmMetadata");
            Assert.IsNull(testArticle.XpmPropertyMetadata, "testArticle.XpmPropertyMetadata");
            Assert.AreEqual(string.Empty, testArticle.GetXpmMarkup(testLocalization), "testArticle.GetXpmMarkup(testLocalization)");
        }


        [TestMethod]
        public void GetPageModel_RichTextImageWithHtmlClass_Success() // See TSI-1614
        {
            string testPageUrlPath = TestFixture.Tsi1614PageUrlPath;

            PageModel pageModel = _testContentProvider.GetPageModel(testPageUrlPath, TestFixture.ParentLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            Article testArticle = pageModel.Regions["Main"].Entities[0] as Article;
            Assert.IsNotNull(testArticle, "Test Article not found on Page.");
            Image testImage = testArticle.ArticleBody[0].Content.Fragments.OfType<Image>().FirstOrDefault();
            Assert.IsNotNull(testImage, "Test Image not found in Rich Text");
            Assert.AreEqual("test tsi1614", testImage.HtmlClasses, "Image.HtmlClasses");
        }       

        [TestMethod]
        public void GetPageModel_InternationalizedUrl_Success() // See TSI-1278
        {
            string testPageUrlPath = TestFixture.Tsi1278PageUrlPath;

            PageModel pageModel = _testContentProvider.GetPageModel(testPageUrlPath, TestFixture.ParentLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            MediaItem testImage = pageModel.Regions["Main"].Entities[0] as MediaItem;
            Assert.IsNotNull(testImage, "testImage");
            StringAssert.Contains(testImage.Url, "tr%C3%A5dl%C3%B8st", "testImage.Url");
        }

        [TestMethod]
        public void GetPageModel_EmbeddedEntityModels_Success() // See TSI-1758
        {
            string testPageUrlPath = TestFixture.Tsi1758PageUrlPath;

            PageModel pageModel = _testContentProvider.GetPageModel(testPageUrlPath, TestFixture.ParentLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
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
            PageModel pageModel = _testContentProvider.GetPageModel(TestFixture.Tsi1946PageUrlPath, TestFixture.ParentLocalization, addIncludes: false);

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
            Localization testLocalization = TestFixture.ParentLocalization;

            Tsi811PageModel pageModel = _testContentProvider.GetPageModel(TestFixture.Tsi811PageUrlPath, testLocalization, addIncludes: false) as Tsi811PageModel;

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
        public void GetPageModel_Meta_Success() // See TSI-1308
        {
            PageModel pageModel = _testContentProvider.GetPageModel(TestFixture.Tsi1308PageUrlPath, TestFixture.ParentLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            OutputJson(pageModel);

            IDictionary<string, string> pageMeta = pageModel.Meta;
            Assert.IsNotNull(pageMeta, "pageMeta");
            Assert.AreEqual(15, pageMeta.Count, "pageMeta.Count");
            Assert.AreEqual("This is single line text", pageMeta["singleLineText"], "pageMeta[singleLineText]");
            Assert.AreEqual("This is multi line text line 1\nAnd line 2\n", pageMeta["multiLineText"], "pageMeta[multiLineText]");
            Assert.AreEqual("This is <strong>rich</strong> text with a <a title=\"Test Article\" href=\"/autotest-parent/test_article_dynamic\">Component Link</a>", pageMeta["richText"], "pageMeta[richText]");
            Assert.AreEqual("News Article", pageMeta["keyword"], "pageMeta[keyword]");
            Assert.AreEqual("/autotest-parent/test_article_dynamic", pageMeta["componentLink"], "pageMeta[componentLink]");
            Assert.AreEqual("/autotest-parent/Images/company-news-placeholder_tcm1065-4480.png", pageMeta["mmComponentLink"], "pageMeta[mmComponentLink]");
            Assert.AreEqual("1970-12-16T12:34:56", pageMeta["date"], "pageMeta[date]");
            Assert.AreEqual("666.666", pageMeta["number"], "pageMeta[number]");
            Assert.AreEqual("Rick Pannekoek", pageMeta["author"], "pageMeta[author]");
            Assert.AreEqual("TSI-1308 Test Page", pageMeta["og:title"], "pageMeta[og: title]");
            Assert.AreEqual("TSI-1308 Test Page", pageMeta["description"], "pageMeta[description]");
        }

        [TestMethod]
        public void GetPageModel_RetrofitMapping_Success() // See TSI-1757
        {
            PageModel pageModel = _testContentProvider.GetPageModel(TestFixture.Tsi1757PageUrlPath, TestFixture.ChildLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            OutputJson(pageModel);

            Tsi1757TestEntity3 testEntity3 = pageModel.Regions["Main"].Entities[0] as Tsi1757TestEntity3;
            Assert.IsNotNull(testEntity3, "testEntity3");

            Assert.AreEqual("This is the textField of TSI-1757 Test Component 3", testEntity3.TextField, "testEntity3.TextField");
            Assert.IsNotNull(testEntity3.CompLinkField, "testEntity3.CompLinkField");
            Assert.AreEqual(2, testEntity3.CompLinkField.Count, "testEntity3.CompLinkField.Count");

            Tsi1757TestEntity1 testEntity1 = testEntity3.CompLinkField[0] as Tsi1757TestEntity1;
            Assert.IsNotNull(testEntity1, "testEntity1");
            Assert.AreEqual("This is the textField of TSI-1757 Test Component 1", testEntity1.TextField, "testEntity1.TextField");
            Assert.AreEqual("This is the embeddedTextField of TSI-1757 Test Component 1", testEntity1.EmbeddedTextField, "testEntity1.EmbeddedTextField");

            Tsi1757TestEntity2 testEntity2 = testEntity3.CompLinkField[1] as Tsi1757TestEntity2;
            Assert.IsNotNull(testEntity2, "testEntity2");
            Assert.AreEqual("This is the textField of TSI-1757 Test Component 2", testEntity2.TextField, "testEntity2.TextField");
        }

        [TestMethod]
        public void PopulateDynamicList_TeaserFallbackToDescription_Success() // See TSI-1852
        {
            string testPageUrlPath = TestFixture.Tsi1852PageUrlPath;

            PageModel pageModel = _testContentProvider.GetPageModel(testPageUrlPath, TestFixture.ParentLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            ContentList<Teaser> testContentList = pageModel.Regions["Main"].Entities[0] as ContentList<Teaser>;
            Assert.IsNotNull(testContentList, "testContentList");
            Assert.IsNotNull(testContentList.ItemListElements, "testContentList.ItemListElements");
            Assert.AreEqual(0, testContentList.ItemListElements.Count, "testContentList.ItemListElements is not empty before PopulateDynamicList");

            _testContentProvider.PopulateDynamicList(testContentList, TestFixture.ParentLocalization);

            Teaser testTeaser = testContentList.ItemListElements.FirstOrDefault(t => t.Headline == "TSI-1852 Article");
            Assert.IsNotNull(testTeaser, "Test Teaser not found");
            StringAssert.StartsWith(testTeaser.Text.ToString(), "This is the standard metadata description", "testTeaser.Text");
        }
    }
}
