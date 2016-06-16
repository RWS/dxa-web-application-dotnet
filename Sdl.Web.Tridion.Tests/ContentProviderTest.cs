using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class ContentProviderTest : TestClass
    {
        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }

        [TestMethod]
        public void GetPageModel_InternationalizedUrl_Success() // See TSI-1278
        {
            string testPathUrlPath = TestFixture.Tsi1278PageUrlPath;

            PageModel pageModel = SiteConfiguration.ContentProvider.GetPageModel(testPathUrlPath, TestFixture.ParentLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            Image testImage = pageModel.Regions["Main"].Entities[0] as Image;
            Assert.IsNotNull(testImage, "testImage");
            StringAssert.Contains(testImage.Url, "tr%C3%A5dl%C3%B8st", "testImage.Url");
        }

        [TestMethod, Ignore] // TODO: It seems that DD4T can't deal with internationalized URLs.
        public void GetStaticContentItem_InternationalizedUrl_Success() 
        {
            string testStaticContentItemUrlPath = TestFixture.Tsi1278StaticContentItemUrlPath;

            StaticContentItem staticContentItem = SiteConfiguration.ContentProvider.GetStaticContentItem(testStaticContentItemUrlPath, TestFixture.ParentLocalization);

            Assert.IsNotNull(staticContentItem, "staticContentItem");
        }



        [TestMethod]
        public void GetPageModel_XpmMarkup_Success()
        {
            string testPageUrlPath = TestFixture.ArticlePageUrlPath;

            PageModel pageModel = SiteConfiguration.ContentProvider.GetPageModel(testPageUrlPath, TestFixture.ParentLocalization, addIncludes: false);

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

            PageModel pageModel = SiteConfiguration.ContentProvider.GetPageModel(testPageUrlPath, TestFixture.ParentLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            Article testArticle = pageModel.Regions["Main"].Entities[0] as Article;
            Assert.IsNotNull(testArticle, "Test Article not found on Page.");
            Image testImage = testArticle.ArticleBody[0].Content.Fragments.OfType<Image>().FirstOrDefault();
            Assert.IsNotNull(testImage, "Test Image not found in Rich Text");
            Assert.AreEqual("test tsi1614", testImage.HtmlClasses, "Image.HtmlClasses");
        }

        [TestMethod]
        public void GetEntityModel_XpmMarkup_Success()
        {
            string testEntityId = TestFixture.ArticleDcpEntityId;

            EntityModel entityModel = SiteConfiguration.ContentProvider.GetEntityModel(testEntityId, TestFixture.ParentLocalization);

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
        public void GetEntityModel_NonExistent_Exception()
        {
            const string testEntityId = "666-666"; // Should not actually exist
            SiteConfiguration.ContentProvider.GetEntityModel(testEntityId, TestFixture.ParentLocalization);
        }
    }
}
