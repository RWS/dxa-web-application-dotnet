using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common.Models;
using Sdl.Web.Tridion.Tests.Models;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class ViewModelTest : TestClass
    {
        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }

        [TestMethod]
        public void DeepCopy_Success()
        {
            Article testArticle = new Article() { Id = "666-666" };
            testArticle.XpmPropertyMetadata = new Dictionary<string, string> { { "xxx", "yyy" } };
            RegionModel testRegionModel = new RegionModel("test");
            testRegionModel.Entities.Add(testArticle);
            PageModel testPageModel = new PageModel("666") { Url = "/test" };
            testPageModel.Regions.Add(testRegionModel);
            testPageModel.Meta.Add("aaa", "bbb");
            testPageModel.XpmMetadata = new Dictionary<string, object> { {"ccc", "ddd"}, {"eee", 666} };
            testPageModel.ExtensionData = new Dictionary<string, object> { { "fff", "ggg" }, { "hhh", 6666 } };
            OutputJson(testPageModel);

            PageModel clonedPageModel = testPageModel.DeepCopy() as PageModel;
            Assert.IsNotNull(clonedPageModel, "clonedPageModel");
            OutputJson(clonedPageModel);

            Assert.AreNotSame(testPageModel, clonedPageModel, "clonedPageModel");
            Assert.AreEqual(testPageModel.Id, clonedPageModel.Id, "clonedPageModel.Id");
            Assert.AreEqual(testPageModel.Url, clonedPageModel.Url, "clonedPageModel.Url");
            AssertEqualCollections(testPageModel.Meta, clonedPageModel.Meta, "clonedPageModel.Meta");
            AssertEqualCollections(testPageModel.XpmMetadata, clonedPageModel.XpmMetadata, "clonedPageModel.XpmMetadata");
            AssertEqualCollections(testPageModel.ExtensionData, clonedPageModel.ExtensionData, "clonedPageModel.ExtensionData");
            AssertEqualCollections(testPageModel.Regions, clonedPageModel.Regions, "clonedPageModel.Regions");

            RegionModel clonedRegionModel = clonedPageModel.Regions[testRegionModel.Name];
            Assert.IsNotNull(clonedRegionModel, "clonedRegionModel");
            Assert.AreNotSame(testRegionModel, clonedRegionModel, "clonedRegionModel");
            AssertEqualCollections(testRegionModel.Entities, clonedRegionModel.Entities, "clonedRegionModel.Entities");
            AssertEqualCollections(testRegionModel.Regions, clonedRegionModel.Regions, "clonedRegionModel.Regions");

            Article clonedArticle = clonedRegionModel.Entities[0] as Article;
            Assert.IsNotNull(clonedArticle, "clonedArticle");
            Assert.AreNotSame(testArticle, clonedArticle, "clonedArticle");
            Assert.AreEqual(testArticle.Id, clonedArticle.Id, "clonedArticle.Id");
            AssertEqualCollections(testArticle.XpmPropertyMetadata, clonedArticle.XpmPropertyMetadata, "clonedArticle.XpmPropertyMetadata");
        }
    }
}
