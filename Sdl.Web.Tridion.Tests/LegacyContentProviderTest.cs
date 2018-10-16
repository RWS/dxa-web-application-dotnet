using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common.Models;
using Sdl.Web.Tridion.Tests.Models;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Tridion.Mapping;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class LegacyContentProviderTest : ContentProviderTest
    {
        public LegacyContentProviderTest()
            : base(new GraphQLContentProvider(), () => TestFixture.LegacyParentLocalization)
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
            string testEntityId = GetArticleDcpEntityId();
            ILocalization testLocalization = TestFixture.LegacyChildLocalization;

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
            ILocalization testLocalization = TestFixture.LegacyChildLocalization;
            string testPageUrlPath =  testLocalization.GetAbsoluteUrlPath(TestFixture.ArticlePageRelativeUrlPath);

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


    }
}
