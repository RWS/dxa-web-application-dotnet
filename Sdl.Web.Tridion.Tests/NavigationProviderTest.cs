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
    public class NavigationProviderTest : TestClass
    {
        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }

        [TestMethod]
        public void GetNavigationModel_Success()
        {
            SitemapItem testSitemap = SiteConfiguration.NavigationProvider.GetNavigationModel(TestFixture.ParentLocalization);

            Assert.IsNotNull(testSitemap, "Result of NavigationProvider.GetNavigationModel");
            // TODO: further assertions
        }

    }
}
