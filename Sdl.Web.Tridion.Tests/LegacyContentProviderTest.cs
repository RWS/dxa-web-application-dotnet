using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common;
using Sdl.Web.Common.Models;
using Sdl.Web.Tridion.Tests.Models;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class LegacyContentProviderTest : ContentProviderTest
    {
        public LegacyContentProviderTest()
            : base(new Mapping.DefaultContentProvider(), () => TestFixture.ParentLocalization)
        {
        }

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }

        [TestMethod]
        public void GetStaticContentItem_NonExistent_Exception()
        {
            const string testStaticContentItemUrlPath = "/does/not/exist";
            AssertThrowsException<DxaItemNotFoundException>(() => TestContentProvider.GetStaticContentItem(testStaticContentItemUrlPath, TestFixture.ParentLocalization));
        }

        [TestMethod]
        public void GetEntityModel_XpmMetadataOnStaging_Success()
        {
            const string testEntityId = TestFixture.ArticleDcpEntityId;

            EntityModel entityModel = TestContentProvider.GetEntityModel(testEntityId, TestFixture.ParentLocalization);

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

            EntityModel entityModel = TestContentProvider.GetEntityModel(testEntityId, testLocalization);

            Assert.IsNotNull(entityModel, "entityModel");
            Assert.AreEqual(testEntityId, entityModel.Id, "entityModel.Id");
            Assert.IsNull(entityModel.XpmMetadata, "entityModel.XpmMetadata");
            Assert.IsNull(entityModel.XpmPropertyMetadata, "entityModel.XpmPropertyMetadata");
            Assert.AreEqual(string.Empty, entityModel.GetXpmMarkup(testLocalization), "entityModel.GetXpmMarkup(testLocalization)");
        }

        [TestMethod]
        [ExpectedException(typeof(DxaItemNotFoundException))]
        public void GetEntityModel_NonExistent_Exception()
        {
            const string testEntityId = "666-666"; // Should not actually exist
            TestContentProvider.GetEntityModel(testEntityId, TestFixture.ParentLocalization);
        }

        [TestMethod]
        [ExpectedException(typeof(DxaException))]
        public void GetEntityModel_InvalidId_Exception()
        {
            TestContentProvider.GetEntityModel("666", TestFixture.ParentLocalization);
        }

        [TestMethod]
        public void GetPageModel_RichTextProcessing_Success()
        {
            string testPageUrlPath = TestFixture.ArticlePageUrlPath;

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestFixture.ParentLocalization, addIncludes: false);

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
            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, testLocalization, addIncludes: false);
            Assert.IsNotNull(pageModel, "pageModel");
            Assert.AreEqual(1, pageModel.Regions["Main"].Entities.Count, "pageModel.Regions[Main].Entities.Count");
            Article testArticle = (Article) pageModel.Regions["Main"].Entities[0];

            try
            {
                MockConditionalEntityEvaluator.EvaluatedEntities.Clear();
                MockConditionalEntityEvaluator.ExcludeEntityIds.Add(testArticle.Id);

                // Test Page's Article should now be suppressed by MockConditionalEntityEvaluator
                PageModel pageModel2 = TestContentProvider.GetPageModel(testPageUrlPath, testLocalization, addIncludes: false);
                Assert.IsNotNull(pageModel2, "pageModel2");
                Assert.AreEqual(0, pageModel2.Regions["Main"].Entities.Count, "pageModel2.Regions[Main].Entities.Count");
                Assert.AreEqual(1, MockConditionalEntityEvaluator.EvaluatedEntities.Count, "MockConditionalEntityEvaluator.EvaluatedEntities.Count");
            }
            finally
            {
                MockConditionalEntityEvaluator.ExcludeEntityIds.Clear();
            }

            // Verify post-test state: Test Page should still contain 1 Article
            PageModel pageModel3 = TestContentProvider.GetPageModel(testPageUrlPath, testLocalization, addIncludes: false);
            Assert.IsNotNull(pageModel3, "pageModel3");
            Assert.AreEqual(1, pageModel3.Regions["Main"].Entities.Count, "pageModel3.Regions[Main].Entities.Count");
        }

        [TestMethod]
        public void GetPageModel_NoXpmMetadataOnLive_Success() // See TSI-1942
        {
            string testPageUrlPath = TestFixture.ArticleChildPageUrlPath;
            Localization testLocalization = TestFixture.ChildLocalization;

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, testLocalization, addIncludes: true);

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

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestFixture.ParentLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            Article testArticle = pageModel.Regions["Main"].Entities[0] as Article;
            Assert.IsNotNull(testArticle, "Test Article not found on Page.");
            Image testImage = testArticle.ArticleBody[0].Content.Fragments.OfType<Image>().FirstOrDefault();
            Assert.IsNotNull(testImage, "Test Image not found in Rich Text");
            Assert.AreEqual("test tsi1614", testImage.HtmlClasses, "Image.HtmlClasses");
        }       

        [TestMethod]
        public void GetPageModel_RetrofitMapping_Success() // See TSI-1757
        {
            PageModel pageModel = TestContentProvider.GetPageModel(TestFixture.Tsi1757PageUrlPath, TestFixture.ChildLocalization, addIncludes: false);

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
        public void GetPageModel_LanguageSelector_Success() // See TSI-2225
        {
            PageModel pageModel = TestContentProvider.GetPageModel(TestFixture.Tsi2225PageUrlPath, TestFixture.ParentLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            OutputJson(pageModel);

            Common.Models.Configuration configEntity = pageModel.Regions["Nav"].Entities[0] as Common.Models.Configuration;
            Assert.IsNotNull(configEntity, "configEntity");
            Assert.AreEqual("tcm:1065-9712", configEntity.Settings["defaultContentLink"], "configEntity.Settings['defaultCOntentLink']");
            Assert.AreEqual("pt,mx", configEntity.Settings["suppressLocalizations"], "configEntity.Settings['suppressLocalizations']");
        }


        [TestMethod]
        public void PopulateDynamicList_TeaserFallbackToDescription_Success() // See TSI-1852
        {
            string testPageUrlPath = TestFixture.Tsi1852PageUrlPath;

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestFixture.ParentLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            ContentList<Teaser> testContentList = pageModel.Regions["Main"].Entities[0] as ContentList<Teaser>;
            Assert.IsNotNull(testContentList, "testContentList");
            Assert.IsNotNull(testContentList.ItemListElements, "testContentList.ItemListElements");
            Assert.AreEqual(0, testContentList.ItemListElements.Count, "testContentList.ItemListElements is not empty before PopulateDynamicList");

            TestContentProvider.PopulateDynamicList(testContentList, TestFixture.ParentLocalization);

            Teaser testTeaser = testContentList.ItemListElements.FirstOrDefault(t => t.Headline == "TSI-1852 Article");
            Assert.IsNotNull(testTeaser, "Test Teaser not found");
            StringAssert.StartsWith(testTeaser.Text.ToString(), "This is the standard metadata description", "testTeaser.Text");
        }
    }
}
