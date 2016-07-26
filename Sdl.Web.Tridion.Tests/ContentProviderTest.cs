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
        public void GetPageModel_ImplicitIndexPage_Success() 
        {
            string testPageUrlPath = TestFixture.ParentLocalization.Path; // Implicitly address the home page (index.html)

            PageModel pageModel = SiteConfiguration.ContentProvider.GetPageModel(testPageUrlPath, TestFixture.ParentLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            Assert.AreEqual(TestFixture.HomePageId, pageModel.Id, "Id");
        }

        [TestMethod]
        public void GetStaticContentItem_InternationalizedUrl_Success() // See TSI-1279 and TSI-1495
        {
            string testStaticContentItemUrlPath = TestFixture.Tsi1278StaticContentItemUrlPath;

            StaticContentItem staticContentItem = SiteConfiguration.ContentProvider.GetStaticContentItem(testStaticContentItemUrlPath, TestFixture.ParentLocalization);

            Assert.IsNotNull(staticContentItem, "staticContentItem");
        }             
      
        [TestMethod]
        [ExpectedException(typeof(DxaItemNotFoundException))]
        public void GetPageModel_NonExistent_Exception()
        {
            SiteConfiguration.ContentProvider.GetPageModel("/does/not/exist", TestFixture.ParentLocalization);
        }

        [TestMethod]
        [ExpectedException(typeof(DxaItemNotFoundException))]
        public void GetEntityModel_NonExistent_Exception()
        {
            const string testEntityId = "666-666"; // Should not actually exist
            SiteConfiguration.ContentProvider.GetEntityModel(testEntityId, TestFixture.ParentLocalization);
        }

        [TestMethod]
        [ExpectedException(typeof(DxaException))]
        public void GetEntityModel_InvalidId_Exception()
        {
            SiteConfiguration.ContentProvider.GetEntityModel("666", TestFixture.ParentLocalization);
        }

    }
}
