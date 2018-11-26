using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using Sdl.Web.Tridion.Navigation;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class StaticNavigationProviderTest : TestClass
    {
        private static readonly INavigationProvider _testNavigationProvider = new StaticNavigationProvider();

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
            Assert.AreEqual("Home", rootSitemapItem.Title, "Root SitemapItem.Title");
            Assert.AreEqual("StructureGroup", rootSitemapItem.Type, "Root SitemapItem.Type");
            Assert.AreEqual(testLocalization.Path + "/", rootSitemapItem.Url, "Root SitemapItem.Url");
            Assert.IsFalse(rootSitemapItem.Visible, "Root SitemapItem.Visible");
            Assert.IsNull(rootSitemapItem.PublishedDate, "Root SitemapItem.PublishedDate");
            Assert.IsTrue(rootSitemapItem.Items.Count > 0, "Root SitemapItem.Items (count)");

            SitemapItem homePageSitemapItem = rootSitemapItem.Items.FirstOrDefault(si => si.Type == "Page" && si.Title == "Home");
            Assert.IsNotNull(homePageSitemapItem, "Home Page SitemapItem");
            Assert.AreEqual(rootSitemapItem.Url + "index", homePageSitemapItem.Url, "Home Page SitemapItem.Url");
            Assert.IsTrue(homePageSitemapItem.Visible, "Root SitemapItem.Visible");
            Assert.IsNotNull(homePageSitemapItem.PublishedDate, "Home Page SitemapItem.PublishedDate");
        }

        [TestMethod]
        public void GetTopNavigationLinks_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;

            NavigationLinks testNavLinks = _testNavigationProvider.GetTopNavigationLinks(testLocalization.Path, testLocalization);

            Assert.IsNotNull(testNavLinks, "NavigationLinks");
            AssertValidHomePageLink(testNavLinks, testLocalization);
        }


        [TestMethod]
        public void GetContextNavigationLinks_Root_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;

            NavigationLinks navLinks = _testNavigationProvider.GetContextNavigationLinks(testLocalization.Path, testLocalization);

            Assert.IsNotNull(navLinks, "navLinks");
            OutputJson(navLinks);

            Assert.AreEqual(1, navLinks.Items.Count, "navLinks.Items.Count");
            AssertValidLink(navLinks.Items[0], testLocalization.Path + "/index", "Home", null, "navLinks.Items[0]");
        }

        [TestMethod]
        public void GetContextNavigationLinks_TaxonomyTestPage1_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;
            string testUrlPath = testLocalization.GetAbsoluteUrlPath(TestFixture.TaxonomyTestPage1RelativeUrlPath);

            NavigationLinks navLinks = _testNavigationProvider.GetContextNavigationLinks(testUrlPath, testLocalization);

            Assert.IsNotNull(navLinks, "navLinks");
            OutputJson(navLinks);

            Assert.AreEqual(3, navLinks.Items.Count, "navLinks.Items.Count");
            AssertValidLink(navLinks.Items[0], testLocalization.Path + "/regression/taxonomy/index", "Navigation Taxonomy Index Page", null, "navLinks.Items[0]");
            AssertValidLink(navLinks.Items[1], testLocalization.Path + "/regression/taxonomy/nav-taxonomy-test-2", "Navigation Taxonomy Test Page 2", null, "navLinks.Items[1]");
            AssertValidLink(navLinks.Items[2], testLocalization.Path + "/regression/taxonomy/nav-taxonomy-test-1", "Navigation Taxonomy Test Page 1", null, "navLinks.Items[2]");
        }


        [TestMethod]
        public void GetBreadcrumbNavigationLinks_Root_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;

            NavigationLinks navLinks = _testNavigationProvider.GetBreadcrumbNavigationLinks(testLocalization.Path, testLocalization);

            Assert.IsNotNull(navLinks, "navLinks");
            OutputJson(navLinks);

            Assert.AreEqual(1, navLinks.Items.Count, "navLinks.Items.Count");
            AssertValidLink(navLinks.Items[0], testLocalization.Path + "/", "Home", null, "navLinks.Items[0]");
        }

        [TestMethod]
        public void GetBreadcrumbNavigationLinks_TaxonomyTestPage1_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;
            string testUrlPath = testLocalization.GetAbsoluteUrlPath(TestFixture.TaxonomyTestPage1RelativeUrlPath);

            NavigationLinks navLinks = _testNavigationProvider.GetBreadcrumbNavigationLinks(testUrlPath, testLocalization);

            Assert.IsNotNull(navLinks, "navLinks");
            OutputJson(navLinks);

            Assert.AreEqual(4, navLinks.Items.Count, "navLinks.Items.Count");
            AssertValidLink(navLinks.Items[0], testLocalization.Path + "/", "Home", null, "navLinks.Items[0]");
            AssertValidLink(navLinks.Items[1], testLocalization.Path + "/regression", "Regression", null, "navLinks.Items[1]");
            AssertValidLink(navLinks.Items[2], testLocalization.Path + "/regression/taxonomy", "Taxonomy", null, "navLinks.Items[2]");
            AssertValidLink(navLinks.Items[3], testLocalization.Path + "/regression/taxonomy/nav-taxonomy-test-1", "Navigation Taxonomy Test Page 1", null, "navLinks.Items[3]");
        }


        private void AssertValidHomePageLink(NavigationLinks navLinks, Localization testLocalization)
        {
            Link homePageLink = navLinks.Items.FirstOrDefault(link => link.LinkText == "Home");
            Assert.IsNotNull(homePageLink, "Home Page Link not found");
            Assert.AreEqual("Home", homePageLink.LinkText, "Home Page Link.LinkText");
            Assert.AreEqual(testLocalization.Path + "/index", homePageLink.Url, "Home Page Link.Url");
        }
    }
}
