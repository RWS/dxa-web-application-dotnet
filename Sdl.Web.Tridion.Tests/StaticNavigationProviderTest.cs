using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class StaticNavigationProviderTest : TestClass
    {
        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }

        [TestMethod]
        public void GetNavigationModel_Success()
        {
            SitemapItem rootSitemapItem = SiteConfiguration.NavigationProvider.GetNavigationModel(TestFixture.ParentLocalization);

            Assert.IsNotNull(rootSitemapItem, "Result of NavigationProvider.GetNavigationModel");
            Assert.AreEqual("Home", rootSitemapItem.Title, "Root SitemapItem Title");
            Assert.AreEqual("StructureGroup", rootSitemapItem.Type, "Root SitemapItem Type");
            Assert.AreEqual(TestFixture.ParentLocalization.Path + "/", rootSitemapItem.Url, "Root SitemapItem Url");
        }

    }
}
