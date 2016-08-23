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
            string testPageUrlPath = TestFixture.ParentLocalization.Path; // Implicitly address the home page (index.html)

            PageModel pageModel = _testContentProvider.GetPageModel(testPageUrlPath, TestFixture.ParentLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            Assert.AreEqual(TestFixture.HomePageId, pageModel.Id, "Id");
        }

        [TestMethod]
        public void GetStaticContentItem_InternationalizedUrl_Success() // See TSI-1279 and TSI-1495
        {
            string testStaticContentItemUrlPath = TestFixture.Tsi1278StaticContentItemUrlPath;

            StaticContentItem staticContentItem = _testContentProvider.GetStaticContentItem(testStaticContentItemUrlPath, TestFixture.ParentLocalization);

            Assert.IsNotNull(staticContentItem, "staticContentItem");
        }

        [TestMethod]       
        public void GetEntityModel_XpmMarkup_Success()
        {
            string testEntityId = TestFixture.ArticleDcpEntityId;

            EntityModel entityModel = _testContentProvider.GetEntityModel(testEntityId, TestFixture.ParentLocalization);

            Assert.IsNotNull(entityModel, "entityModel");
            Assert.AreEqual(testEntityId, entityModel.Id, "entityModel.Id");
            Assert.IsNotNull(entityModel.XpmMetadata, "entityModel.XpmMetadata");
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
        public void GetPageModel_XpmMarkup_Success()
        {
            string testPageUrlPath = TestFixture.ArticlePageUrlPath;

            PageModel pageModel = _testContentProvider.GetPageModel(testPageUrlPath, TestFixture.ParentLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");

            Article testArticle = pageModel.Regions["Main"].Entities[0] as Article;
            Assert.IsNotNull(testArticle, "Test Article not found on Page.");

            Assert.IsNotNull(testArticle.XpmMetadata, "entityModel.XpmMetadata");
            object isQueryBased;
            Assert.IsFalse(testArticle.XpmMetadata.TryGetValue("IsQueryBased", out isQueryBased), "XpmMetadata contains 'IsQueryBased'");
            object isRepositoryPublished;
            Assert.IsTrue(testArticle.XpmMetadata.TryGetValue("IsRepositoryPublished", out isRepositoryPublished), "XpmMetadata contains 'IsRepositoryPublished'");
            Assert.AreEqual(false, isRepositoryPublished, "IsRepositoryPublished value");

            // NOTE: boolean value must not have quotes in XPM markup (TSI-1251)
            string xpmMarkup = testArticle.GetXpmMarkup(TestFixture.ParentLocalization);
            StringAssert.DoesNotMatch(xpmMarkup, new Regex("IsQueryBased"), "XPM markup");
            StringAssert.Contains(xpmMarkup, "\"IsRepositoryPublished\":false", "XPM markup");
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
