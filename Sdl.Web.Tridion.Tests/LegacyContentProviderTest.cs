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
