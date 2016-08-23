using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Entity;
using Sdl.Web.Tridion.Navigation;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class DynamicNavigationProviderTest : TestClass
    {
        private static readonly INavigationProvider _testNavigationProvider = new DynamicNavigationProvider();

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }

        [TestMethod]
        public void GetNavigationModel_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;

            SitemapItem rootSitemapItem = _testNavigationProvider.GetNavigationModel(testLocalization);

            Assert.IsNotNull(rootSitemapItem, "Root SitemapItem");
            Console.WriteLine(JsonConvert.SerializeObject(rootSitemapItem, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

            Assert.IsTrue(rootSitemapItem is TaxonomyNode, "rootSitemapItem should be of type TaxonomyNode");
            Assert.AreEqual("Category", rootSitemapItem.Type, "rootSitemapItem.Type");
            Assert.AreEqual("Test Taxonomy [Navigation]", rootSitemapItem.Title);
            // TODO: further assertions
        }

        /*

        [TestMethod]
        public void GetTopNavigationLinks_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;

            NavigationLinks testNavLinks = _testNavigationProvider.GetTopNavigationLinks(testLocalization.Path, testLocalization);

            Assert.IsNotNull(testNavLinks, "NavigationLinks");
            AssertValidHomePageLink(testNavLinks, testLocalization);
        }


        [TestMethod]
        public void GetContextNavigationLinks_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;

            NavigationLinks testNavLinks = _testNavigationProvider.GetContextNavigationLinks(testLocalization.Path, testLocalization);

            Assert.IsNotNull(testNavLinks, "NavigationLinks");
            AssertValidHomePageLink(testNavLinks, testLocalization);
        }

        [TestMethod]
        public void GetBreadcrumbNavigationLinks_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;

            NavigationLinks testNavLinks = _testNavigationProvider.GetBreadcrumbNavigationLinks(testLocalization.Path + "index", testLocalization);

            Assert.IsNotNull(testNavLinks, "NavigationLinks");
            Assert.AreEqual(1, testNavLinks.Items.Count, "NavigationLinks.Items (count)");

            Link rootStructureGroupLink = testNavLinks.Items[0];
            Assert.AreEqual("Home", rootStructureGroupLink.LinkText, "Root SG Link.LinkText");
            Assert.AreEqual(testLocalization.Path + "/", rootStructureGroupLink.Url, "Root SG Link.Url");
        }

        private void AssertValidHomePageLink(NavigationLinks navLinks, Localization testLocalization)
        {
            Link homePageLink = navLinks.Items.FirstOrDefault(link => link.LinkText == "Home");
            Assert.IsNotNull(homePageLink, "Home Page Link not found");
            Assert.AreEqual("Home", homePageLink.LinkText, "Home Page Link.LinkText");
            Assert.AreEqual(testLocalization.Path + "/index", homePageLink.Url, "Home Page Link.Url");
        }
        */
    }
}
