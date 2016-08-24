using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            OutputJson(rootSitemapItem);

            TaxonomyNode rootNode = rootSitemapItem as TaxonomyNode;
            Assert.IsNotNull(rootNode, "rootSitemapItem is not of type TaxonomyNode");
            StringAssert.Matches(rootNode.Id, new Regex(@"t\d+"), "rootNode.Id");
            Assert.AreEqual(SitemapItem.Types.TaxonomyNode, rootNode.Type, "rootNode.Type");
            Assert.AreEqual("Test Taxonomy [Navigation]", rootNode.Title, "rootNode.Title");
            Assert.AreEqual("Test Taxonomy to be used for Navigation purposes", rootNode.Description, "rootNode.Description");
            Assert.IsNull(rootNode.Url, "rootNode.Url");
            Assert.IsTrue(rootNode.IsAbstract, "rootNode.IsAbstract");
            Assert.IsTrue(rootNode.HasChildNodes, "rootNode.HasChildNodes");
            Assert.AreEqual(3, rootNode.ClassifiedItemsCount, "rootNode.ClassifiedItemsCount");
            Assert.IsNotNull(rootNode.Items, "rootNode.Items");
            Assert.AreEqual(2, rootNode.Items.OfType<TaxonomyNode>().Count(), "rootNode.Items.OfType<TaxonomyNode>().Count()");
            Assert.IsNull(rootNode.RelatedTaxonomyNodeIds, "rootNode.RelatedTaxonomyNodeIds");

            TaxonomyNode topLevelKeyword1 = rootNode.Items.OfType<TaxonomyNode>().FirstOrDefault(i => i.Title == "Top-level Keyword 1");
            Assert.IsNotNull(topLevelKeyword1, "topLevelKeyword1");
            Assert.IsNotNull(topLevelKeyword1.Items, "topLevelKeyword1.Items");
            Assert.IsNull(topLevelKeyword1.Url, "topLevelKeyword1.Url");

            TaxonomyNode keyword12 = topLevelKeyword1.Items.OfType<TaxonomyNode>().FirstOrDefault(i => i.Title == "Keyword 1.2");
            Assert.IsNotNull(keyword12, "keyword12");
            Assert.IsNotNull(keyword12.Items, "keyword12.Items");
            Assert.AreEqual(3, keyword12.ClassifiedItemsCount, "keyword12.ClassifiedItemsCount");
            Assert.AreEqual(3, keyword12.Items.Count, "keyword12.Items.Count");
            for (int i = 0; i < keyword12.Items.Count; i++)
            {
                AssertProperSitemapItemForPage(keyword12.Items[i], string.Format("keyword12.Items[{0}]", i));
            }

            // The Pages should be sorted by CM Page title (incl. sequence prefix), but not have the sequence prefix.
            Assert.AreEqual("Navigation Taxonomy Index Page", keyword12.Items[0].Title, "keyword12.Items[0].Title");
            Assert.AreEqual("Navigation Taxonomy Test Page 2", keyword12.Items[1].Title, "keyword12.Items[1].Title");
            Assert.AreEqual("Navigation Taxonomy Test Page 1", keyword12.Items[2].Title, "keyword12.Items[2].Title");

            // TaxonomyNode should get the URL from its Index Page
            Assert.AreEqual(keyword12.Items[0].Url, keyword12.Url, "keyword12.Url");
        }

        private static void AssertProperSitemapItemForPage(SitemapItem pageSitemapItem, string subject)
        {
            StringAssert.Matches(pageSitemapItem.Id, new Regex(@"t\d+-p\d+"), subject + ".Id");
            Assert.AreEqual(SitemapItem.Types.Page, pageSitemapItem.Type, subject + ".Type");
            Assert.IsNotNull(pageSitemapItem.Title, subject + ".Title");
            Assert.IsNotNull(pageSitemapItem.Url, subject + ".Url");
            Assert.IsNotNull(pageSitemapItem.PublishedDate, subject + ".PublishedDate");
        }

        [TestMethod]
        public void GetTopNavigationLinks_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;

            NavigationLinks navLinks = _testNavigationProvider.GetTopNavigationLinks(testLocalization.Path, testLocalization);

            Assert.IsNotNull(navLinks, "navLinks");
            OutputJson(navLinks);

            Assert.IsNotNull(navLinks.Items, "navLinks.Items");
            Assert.AreEqual(1, navLinks.Items.Count, "navLinks.Items.Count");

            Link keyword2Link = navLinks.Items.FirstOrDefault(link => link.LinkText == "Top-level Keyword 2");
            Assert.IsNotNull(keyword2Link, "keyword2Link");
            Assert.AreEqual("/autotest-parent/regression/taxonomy/index.html", keyword2Link.Url, "keyword2Link.Url");
            Assert.AreEqual("Top-level Keyword 2 (concrete)", keyword2Link.AlternateText, "keyword2Link.AlternateText");
        }

        [TestMethod, Ignore]
        public void GetContextNavigationLinks_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;

            NavigationLinks navLinks = _testNavigationProvider.GetContextNavigationLinks(testLocalization.Path, testLocalization);

            Assert.IsNotNull(navLinks, "navLinks");
            // TODO
        }

        [TestMethod, Ignore]
        public void GetBreadcrumbNavigationLinks_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;

            NavigationLinks navLinks = _testNavigationProvider.GetBreadcrumbNavigationLinks(testLocalization.Path + "index", testLocalization);

            Assert.IsNotNull(navLinks, "navLinks");
            // TODO
        }
    }
}
