using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Entity;
using Sdl.Web.Common.Models.Navigation;
using Sdl.Web.Tridion.Navigation;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class DynamicNavigationProviderTest : TestClass
    {
        private static readonly INavigationProvider _testNavigationProvider = new DynamicNavigationProvider();
        private static readonly IOnDemandNavigationProvider _testOnDemandNavigationProvider = (IOnDemandNavigationProvider) _testNavigationProvider;

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
            Assert.IsFalse(rootNode.Visible, "rootNode.Visible");
            Assert.IsTrue(rootNode.IsAbstract, "rootNode.IsAbstract");
            Assert.IsTrue(rootNode.HasChildNodes, "rootNode.HasChildNodes");
            Assert.AreEqual(3, rootNode.ClassifiedItemsCount, "rootNode.ClassifiedItemsCount");
            Assert.IsNotNull(rootNode.Items, "rootNode.Items");
            Assert.AreEqual(2, rootNode.Items.OfType<TaxonomyNode>().Count(), "rootNode.Items.OfType<TaxonomyNode>().Count()");
            Assert.IsNull(rootNode.RelatedTaxonomyNodeIds, "rootNode.RelatedTaxonomyNodeIds");

            TaxonomyNode topLevelKeyword1 = rootNode.Items.OfType<TaxonomyNode>().FirstOrDefault(i => i.Title == TestFixture.TopLevelKeyword1Title);
            Assert.IsNotNull(topLevelKeyword1, "topLevelKeyword1");
            Assert.IsNotNull(topLevelKeyword1.Items, "topLevelKeyword1.Items");
            Assert.IsNull(topLevelKeyword1.Url, "topLevelKeyword1.Url");
            Assert.IsFalse(topLevelKeyword1.Visible, "topLevelKeyword1.Visible");

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
            string indexPageUrl = keyword12.Items[0].Url;
            Assert.AreEqual(indexPageUrl.Substring(0, indexPageUrl.Length - "/index".Length), keyword12.Url, "keyword12.Url");
            Assert.IsFalse(keyword12.Visible, "keyword12.Visible"); // It has a URL, but no sequence prefix in CM.
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
        public void GetTopNavigationLinks_Root_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;

            NavigationLinks navLinks = _testNavigationProvider.GetTopNavigationLinks(testLocalization.Path, testLocalization);

            Assert.IsNotNull(navLinks, "navLinks");
            OutputJson(navLinks);

            Assert.AreEqual(1, navLinks.Items.Count, "navLinks.Items.Count");
            AssertValidLink(navLinks.Items[0], "/autotest-parent/regression/taxonomy", TestFixture.TopLevelKeyword2Title, "Top-level Keyword 2 (concrete)", "navLinks.Items[0]");
        }

        private static void AssertExpectedLinks(IList<Link> links)
        {
            Assert.IsNotNull(links, "links");
            Assert.AreEqual(3, links.Count, "links.Count");
            AssertValidLink(links[0], "/autotest-parent/regression/taxonomy/index", "Navigation Taxonomy Index Page", null, "links[0]");
            AssertValidLink(links[1], "/autotest-parent/regression/taxonomy/nav-taxonomy-test-2", "Navigation Taxonomy Test Page 2", null, "links[1]");
            AssertValidLink(links[2], "/autotest-parent/regression/taxonomy/nav-taxonomy-test-1", "Navigation Taxonomy Test Page 1", null, "links[2]");
        }

        [TestMethod]
        public void GetContextNavigationLinks_TaxonomyTestPage2_Success()
        {
            const string testUrlPath = TestFixture.TaxonomyTestPage2UrlPath;
            Localization testLocalization = TestFixture.ParentLocalization;

            NavigationLinks navLinks = _testNavigationProvider.GetContextNavigationLinks(testUrlPath, testLocalization);

            Assert.IsNotNull(navLinks, "navLinks");
            OutputJson(navLinks);

            AssertExpectedLinks(navLinks.Items);
        }

        [TestMethod]
        public void GetContextNavigationLinks_TaxonomyIndexPage_Success()
        {
            const string testUrlPathWithoutIndexSuffix = TestFixture.TaxonomyIndexPageUrlPath;
            const string testUrlPathWithIndexSuffix = testUrlPathWithoutIndexSuffix + "/index";
            Localization testLocalization = TestFixture.ParentLocalization;

            NavigationLinks navLinks = _testNavigationProvider.GetContextNavigationLinks(testUrlPathWithoutIndexSuffix, testLocalization);
            NavigationLinks navLinks2 = _testNavigationProvider.GetContextNavigationLinks(testUrlPathWithIndexSuffix, testLocalization);

            Assert.IsNotNull(navLinks, "navLinks");
            Assert.IsNotNull(navLinks, "navLinks2");
            OutputJson(navLinks);
            OutputJson(navLinks2);

            AssertExpectedLinks(navLinks.Items);
            AssertExpectedLinks(navLinks2.Items);
        }

        [TestMethod]
        public void GetContextNavigationLinks_UnclassifiedPage_Success() // See TSI-1916
        {
            Localization testLocalization = TestFixture.ParentLocalization;

            NavigationLinks navLinks = _testNavigationProvider.GetContextNavigationLinks(TestFixture.ArticlePageUrlPath, testLocalization);

            Assert.IsNotNull(navLinks, "navLinks");
            OutputJson(navLinks);

            Assert.IsNotNull(navLinks.Items, "navLinks.Items");
            Assert.AreEqual(0, navLinks.Items.Count, "navLinks.Items.Count");
        }

        [TestMethod]
        public void GetBreadcrumbNavigationLinks_UnclassifiedPage_Success() // See TSI-1916
        {
            Localization testLocalization = TestFixture.ParentLocalization;

            NavigationLinks navLinks = _testNavigationProvider.GetBreadcrumbNavigationLinks(TestFixture.ArticlePageUrlPath, testLocalization);

            Assert.IsNotNull(navLinks, "navLinks");
            OutputJson(navLinks);

            Assert.IsNotNull(navLinks.Items, "navLinks.Items");
            Assert.AreEqual(0, navLinks.Items.Count, "navLinks.Items.Count");
        }

        [TestMethod]
        public void GetBreadcrumbNavigationLinks_TaxonomyTestPage1_Success()
        {
            const string testUrlPath = TestFixture.TaxonomyTestPage1UrlPath;
            Localization testLocalization = TestFixture.ParentLocalization;

            NavigationLinks navLinks = _testNavigationProvider.GetBreadcrumbNavigationLinks(testUrlPath, testLocalization);

            Assert.IsNotNull(navLinks, "navLinks");
            OutputJson(navLinks);

            Assert.AreEqual(4, navLinks.Items.Count, "navLinks.Items.Count");
            AssertValidLink(navLinks.Items[0], null, TestFixture.TopLevelKeyword1Title, "Top-level Keyword 1 (abstract)", "navLinks.Items[0]");
            AssertValidLink(navLinks.Items[1], null, "Keyword 1.1", "First child Keyword of Top-level Keyword 1", "navLinks.Items[1]");
            AssertValidLink(navLinks.Items[2], null, "Keyword 1.1.2", "Second child Keyword of Keyword 1.1", "navLinks.Items[2]");
            AssertValidLink(navLinks.Items[3], "/autotest-parent/regression/taxonomy/nav-taxonomy-test-1", "Navigation Taxonomy Test Page 1", null, "navLinks.Items[3]");
        }

        [TestMethod]
        public void GetNavigationSubtree_InvalidSitemapItemId_Exception()
        {
            AssertThrowsException<DxaException>(
                () => { _testOnDemandNavigationProvider.GetNavigationSubtree("invalid", null, TestFixture.ParentLocalization); }, 
                "GetNavigationSubtree"
                );
            AssertThrowsException<DxaException>(
                () => { _testOnDemandNavigationProvider.GetNavigationSubtree("t666-q666", null, TestFixture.ParentLocalization); },
                "GetNavigationSubtree"
                );
            AssertThrowsException<DxaException>(
                () => { _testOnDemandNavigationProvider.GetNavigationSubtree("t666-k666!", null, TestFixture.ParentLocalization); },
                "GetNavigationSubtree"
                );
        }

        [TestMethod]
        public void GetNavigationSubtree_NonExistingItem_Success()
        {
            NavigationFilter descendantsFilter = new NavigationFilter();
            NavigationFilter ancestorsFilter = new NavigationFilter { IncludeAncestors = true, DescendantLevels = 0 };
            Localization testLocalization = TestFixture.ParentLocalization;

            IEnumerable<SitemapItem> sitemapItems = _testOnDemandNavigationProvider.GetNavigationSubtree("t666-k666", ancestorsFilter, testLocalization);
            Assert.IsNotNull(sitemapItems, "sitemapItems");
            Assert.IsFalse(sitemapItems.Any(), "sitemapItems.Any()");

            IEnumerable<SitemapItem> sitemapItems2 = _testOnDemandNavigationProvider.GetNavigationSubtree("t666-p666", ancestorsFilter, testLocalization);
            Assert.IsNotNull(sitemapItems2, "sitemapItems2");
            Assert.IsFalse(sitemapItems2.Any(), "sitemapItems2.Any()");

            IEnumerable<SitemapItem> sitemapItems3 = _testOnDemandNavigationProvider.GetNavigationSubtree("t666", descendantsFilter, testLocalization);
            Assert.IsNotNull(sitemapItems3, "sitemapItems3");
            Assert.IsFalse(sitemapItems3.Any(), "sitemapItems3.Any()");
        }

        [TestMethod]
        public void GetNavigationSubtree_TaxonomyRootsOnly_Success()
        {
            NavigationFilter testNavFilter = new NavigationFilter();

            SitemapItem[] taxonomyRoots = _testOnDemandNavigationProvider.GetNavigationSubtree(null, testNavFilter, TestFixture.ParentLocalization).ToArray();

            Assert.IsNotNull(taxonomyRoots, "taxonomyRoots");
            OutputJson(taxonomyRoots);

            AssertNoChildItems(taxonomyRoots, "taxonomyRoots");

            TaxonomyNode testTaxonomyRoot = GetTestTaxonomy(taxonomyRoots);
            Assert.AreEqual(true, testTaxonomyRoot.HasChildNodes, "testTaxonomyRoot.HasChildNodes");
            Assert.AreEqual(3, testTaxonomyRoot.ClassifiedItemsCount, "testTaxonomyRoot.ClassifiedItemsCount");
        }

        [TestMethod]
        public void GetNavigationSubtree_TaxonomyRootsAndChildren_Success()
        {
            NavigationFilter testNavFilter = new NavigationFilter { DescendantLevels = 2 };

            SitemapItem[] taxonomyRoots = _testOnDemandNavigationProvider.GetNavigationSubtree(null, testNavFilter, TestFixture.ParentLocalization).ToArray();

            Assert.IsNotNull(taxonomyRoots, "taxonomyRoots");
            OutputJson(taxonomyRoots);

            TaxonomyNode testTaxonomyRoot = GetTestTaxonomy(taxonomyRoots);
            Assert.IsNotNull(testTaxonomyRoot.Items, "testTaxonomyRoot.Items");
            Assert.AreEqual(2, testTaxonomyRoot.Items.Count, "testTaxonomyRoot.Items.Count");
            AssertNoChildItems(testTaxonomyRoot.Items, "testTaxonomyRoot.Items");
        }

        [TestMethod]
        public void GetNavigationSubtree_FullTaxonomies_Success()
        {
            NavigationFilter testNavFilter = new NavigationFilter { DescendantLevels = -1 };

            SitemapItem[] taxonomyRoots = _testOnDemandNavigationProvider.GetNavigationSubtree(null, testNavFilter, TestFixture.ParentLocalization).ToArray();

            Assert.IsNotNull(taxonomyRoots, "taxonomyRoots");
            OutputJson(taxonomyRoots);

            TaxonomyNode testTaxonomyRoot = GetTestTaxonomy(taxonomyRoots);
            Assert.IsNotNull(testTaxonomyRoot.Items, "testTaxonomyRoot.Items");
            Assert.AreEqual(2, testTaxonomyRoot.Items.Count, "testTaxonomyRoot.Items.Count");

            SitemapItem topLevelKeyword1 = testTaxonomyRoot.Items.FirstOrDefault(i => i.Title == TestFixture.TopLevelKeyword1Title);
            Assert.IsNotNull(topLevelKeyword1, "topLevelKeyword1");
            SitemapItem keyword11 = topLevelKeyword1.Items.FirstOrDefault(i => i.Title == TestFixture.Keyword1_1Title);
            Assert.IsNotNull(keyword11, "keyword11");
            SitemapItem keyword112 = keyword11.Items.FirstOrDefault(i => i.Title == "Keyword 1.1.2");
            Assert.IsNotNull(keyword112, "keyword112");
            Assert.IsNotNull(keyword112.Items, "keyword112.Items");
            Assert.AreEqual(1, keyword112.Items.Count, "keyword112.Items.Count");
        }


        [TestMethod]
        public void GetNavigationSubtree_TestTaxonomyChildren_Success()
        {
            TaxonomyNode testTaxonomyRoot = GetTestTaxonomy();
            NavigationFilter testNavFilter = new NavigationFilter { IncludeRelated = true };

            SitemapItem[] childItems = _testOnDemandNavigationProvider.GetNavigationSubtree(testTaxonomyRoot.Id, testNavFilter, TestFixture.ParentLocalization).ToArray();
            Assert.IsNotNull(childItems, "childItems");
            OutputJson(childItems);

            Assert.AreEqual(2, childItems.Length, "childItems.Length");
            AssertNoChildItems(childItems, "childItems");

            TaxonomyNode topLevelKeyword1 = childItems.FirstOrDefault(i => i.Title == TestFixture.TopLevelKeyword1Title) as TaxonomyNode;
            Assert.IsNotNull(topLevelKeyword1, "topLevelkeyword1");
            Assert.IsNotNull(topLevelKeyword1.RelatedTaxonomyNodeIds, "topLevelkeyword1.RelatedTaxonomyNodeIds"); // IncludeRelated = true
            // TODO TSI-1880: Assert.AreEqual(1, topLevelkeyword1.RelatedTaxonomyNodeIds.Count, "topLevelkeyword1.RelatedTaxonomyNodeIds.Count");
        }

        [TestMethod]
        public void GetNavigationSubtree_TestTaxonomy2LevelDescendants_Success()
        {
            TaxonomyNode testTaxonomyRoot = GetTestTaxonomy();
            NavigationFilter testNavFilter = new NavigationFilter { DescendantLevels = 2 };

            SitemapItem[] childItems = _testOnDemandNavigationProvider.GetNavigationSubtree(testTaxonomyRoot.Id, testNavFilter, TestFixture.ParentLocalization).ToArray();
            Assert.IsNotNull(childItems, "childItems");
            OutputJson(childItems);

            TaxonomyNode topLevelKeyword1 = childItems.FirstOrDefault(i => i.Title == TestFixture.TopLevelKeyword1Title) as TaxonomyNode;
            Assert.IsNotNull(topLevelKeyword1, "topLevelKeyword1");
            Assert.IsNotNull(topLevelKeyword1.Items, "topLevelKeyword1.Items");
            AssertNoChildItems(topLevelKeyword1.Items, "topLevelKeyword1.Items");
            Assert.IsNull(topLevelKeyword1.RelatedTaxonomyNodeIds, "topLevelkeyword1.RelatedTaxonomyNodeIds"); // IncludeRelated = false
        }

        [TestMethod]
        public void GetNavigationSubtree_PageDescendants_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;
            TaxonomyNode testTaxonomyRoot = GetTestTaxonomy();
            PageModel testPageModel = SiteConfiguration.ContentProvider.GetPageModel(TestFixture.TaxonomyTestPage1UrlPath, testLocalization);
            string testPageSitemapItemId = string.Format("{0}-p{1}", testTaxonomyRoot.Id, testPageModel.Id);
            NavigationFilter testNavFilter = new NavigationFilter { DescendantLevels = 1 };

            SitemapItem[] childItems = _testOnDemandNavigationProvider.GetNavigationSubtree(testPageSitemapItemId, testNavFilter, testLocalization).ToArray();
            Assert.IsNotNull(childItems, "childItems");
            OutputJson(childItems);

            Assert.AreEqual(0, childItems.Length, "childItems.Length");
        }

        private static void AssertExpectedTaxonomyNode(TaxonomyNode taxonomyNode, string expectedTitle, int expectedNumberOfChildItems, string subjectName)
        {
            Assert.IsNotNull(taxonomyNode, subjectName);
            Assert.AreEqual(expectedTitle, taxonomyNode.Title, subjectName + ".Title");

            if (expectedNumberOfChildItems >= 0)
            {
                Assert.IsNotNull(taxonomyNode.Items, subjectName + ".Items");
                Assert.AreEqual(expectedNumberOfChildItems, taxonomyNode.Items.Count, subjectName + ".Items.Count");
            }
            else
            {
                Assert.IsNull(taxonomyNode.Items, subjectName + ".Items");
            }
        }

        [TestMethod]
        public void GetNavigationSubtree_IncludeAncestorsKeyword_Success()
        {
            TaxonomyNode testTaxonomyRoot = GetTestTaxonomy(null, -1);
            TaxonomyNode testTopLevelKeyword1 = testTaxonomyRoot.Items.FirstOrDefault(i => i.Title == TestFixture.TopLevelKeyword1Title) as TaxonomyNode;
            Assert.IsNotNull(testTopLevelKeyword1, "testTopLevelKeyword1");
            TaxonomyNode testKeyword11 = testTopLevelKeyword1.Items.FirstOrDefault(i => i.Title == TestFixture.Keyword1_1Title) as TaxonomyNode;
            Assert.IsNotNull(testKeyword11, "testKeyword11");
            NavigationFilter testNavFilter = new NavigationFilter { IncludeAncestors = true, DescendantLevels = 0 };

            SitemapItem[] ancestorItems = _testOnDemandNavigationProvider.GetNavigationSubtree(testKeyword11.Id, testNavFilter, TestFixture.ParentLocalization).ToArray();
            Assert.IsNotNull(ancestorItems, "ancestorItems");
            OutputJson(ancestorItems);

            // Result should be the Taxonomy Root only; the ancestor chain is formed using SitemapItem.Items.
            Assert.AreEqual(1, ancestorItems.Length, "ancestorItems.Length");
            TaxonomyNode taxonomyRoot = ancestorItems[0] as TaxonomyNode;
            AssertExpectedTaxonomyNode(taxonomyRoot, testTaxonomyRoot.Title, 1, "taxonomyRoot");
            TaxonomyNode topLevelKeyword1 = taxonomyRoot.Items[0] as TaxonomyNode;
            AssertExpectedTaxonomyNode(topLevelKeyword1, testTopLevelKeyword1.Title, 1, "topLevelKeyword1");

            // This is the context node
            TaxonomyNode keyword11 = topLevelKeyword1.Items[0] as TaxonomyNode;
            AssertExpectedTaxonomyNode(keyword11, testKeyword11.Title, -1, "keyword11");
        }

        [TestMethod]
        public void GetNavigationSubtree_IncludeAncestorsAndChildrenKeyword_Success()
        {
            TaxonomyNode testTaxonomyRoot = GetTestTaxonomy(null, -1);
            TaxonomyNode testTopLevelKeyword1 = testTaxonomyRoot.Items.FirstOrDefault(i => i.Title == TestFixture.TopLevelKeyword1Title) as TaxonomyNode;
            Assert.IsNotNull(testTopLevelKeyword1, "testTopLevelKeyword1");
            TaxonomyNode testKeyword11 = testTopLevelKeyword1.Items.FirstOrDefault(i => i.Title == TestFixture.Keyword1_1Title) as TaxonomyNode;
            Assert.IsNotNull(testKeyword11, "testKeyword11");
            NavigationFilter testNavFilter = new NavigationFilter { IncludeAncestors = true, DescendantLevels = 1 };

            SitemapItem[] ancestorItems = _testOnDemandNavigationProvider.GetNavigationSubtree(testKeyword11.Id, testNavFilter, TestFixture.ParentLocalization).ToArray();
            Assert.IsNotNull(ancestorItems, "ancestorItems");
            OutputJson(ancestorItems);

            // Result should be the Taxonomy Root only; the ancestor chain is formed using SitemapItem.Items.
            Assert.AreEqual(1, ancestorItems.Length, "ancestorItems.Length");
            TaxonomyNode taxonomyRoot = ancestorItems[0] as TaxonomyNode;
            AssertExpectedTaxonomyNode(taxonomyRoot, testTaxonomyRoot.Title, 2, "taxonomyRoot");
            TaxonomyNode topLevelKeyword1 = taxonomyRoot.Items[0] as TaxonomyNode;
            AssertExpectedTaxonomyNode(topLevelKeyword1, testTopLevelKeyword1.Title, 2, "topLevelKeyword1");

            // This is the context node
            TaxonomyNode keyword11 = topLevelKeyword1.Items[0] as TaxonomyNode;
            AssertExpectedTaxonomyNode(keyword11, testKeyword11.Title, 2, "keyword11");

            // Assert that child nodes are added because of DescendantLevels = 1:
            TaxonomyNode keyword111 = keyword11.Items[0] as TaxonomyNode;
            AssertExpectedTaxonomyNode(keyword111, "Keyword 1.1.1", -1, "keyword111");
            TaxonomyNode keyword12 = topLevelKeyword1.Items[1] as TaxonomyNode;
            AssertExpectedTaxonomyNode(keyword12, "Keyword 1.2", -1, "keyword12");
            TaxonomyNode topLevelKeyword2 = taxonomyRoot.Items[1] as TaxonomyNode;
            AssertExpectedTaxonomyNode(topLevelKeyword2, "Top-level Keyword 2", -1, "topLevelKeyword2");
        }

        [TestMethod]
        public void GetNavigationSubtree_IncludeAncestorsClassifiedPage_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;
            TaxonomyNode testTaxonomyRoot = GetTestTaxonomy(null, -1);
            TaxonomyNode testTopLevelKeyword1 = testTaxonomyRoot.Items.FirstOrDefault(i => i.Title == TestFixture.TopLevelKeyword1Title) as TaxonomyNode;
            Assert.IsNotNull(testTopLevelKeyword1, "testTopLevelKeyword1");
            TaxonomyNode testKeyword11 = testTopLevelKeyword1.Items.FirstOrDefault(i => i.Title == TestFixture.Keyword1_1Title) as TaxonomyNode;
            Assert.IsNotNull(testKeyword11, "testKeyword11");
            PageModel testPageModel = SiteConfiguration.ContentProvider.GetPageModel(TestFixture.TaxonomyTestPage1UrlPath, testLocalization);
            string testPageSitemapItemId = string.Format("{0}-p{1}", testTaxonomyRoot.Id, testPageModel.Id);
            NavigationFilter testNavFilter = new NavigationFilter { IncludeAncestors = true, DescendantLevels = 0 };

            SitemapItem[] ancestorItems = _testOnDemandNavigationProvider.GetNavigationSubtree(testPageSitemapItemId, testNavFilter, testLocalization).ToArray();
            Assert.IsNotNull(ancestorItems, "ancestorItems");
            OutputJson(ancestorItems);

            // Result should be the Taxonomy Root only; it acts as the subtree root for all ancestors.
            Assert.AreEqual(1, ancestorItems.Length, "ancestorItems.Length");
            TaxonomyNode taxonomyRoot = ancestorItems[0] as TaxonomyNode;
            AssertExpectedTaxonomyNode(taxonomyRoot, testTaxonomyRoot.Title, 2, "taxonomyRoot");
            TaxonomyNode topLevelKeyword1 = taxonomyRoot.Items[0] as TaxonomyNode;
            AssertExpectedTaxonomyNode(topLevelKeyword1, testTopLevelKeyword1.Title, 2, "topLevelKeyword1");
            TaxonomyNode keyword11 = topLevelKeyword1.Items[0] as TaxonomyNode;
            AssertExpectedTaxonomyNode(keyword11, testKeyword11.Title, 1, "keyword11");
            TaxonomyNode keyword112 = keyword11.Items[0] as TaxonomyNode;
            AssertExpectedTaxonomyNode(keyword112, "Keyword 1.1.2", -1, "keyword112");
            TaxonomyNode keyword12 = topLevelKeyword1.Items[1] as TaxonomyNode;
            AssertExpectedTaxonomyNode(keyword12, "Keyword 1.2", -1, "keyword12");
            TaxonomyNode topLevelKeyword2 = taxonomyRoot.Items[1] as TaxonomyNode;
            AssertExpectedTaxonomyNode(topLevelKeyword2, TestFixture.TopLevelKeyword2Title, -1, "topLevelKeyword2");
        }

        [TestMethod]
        public void GetNavigationSubtree_IncludeAncestorsAndChildrenClassifiedPage_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;
            TaxonomyNode testTaxonomyRoot = GetTestTaxonomy(null, -1);
            TaxonomyNode testTopLevelKeyword1 = testTaxonomyRoot.Items.FirstOrDefault(i => i.Title == TestFixture.TopLevelKeyword1Title) as TaxonomyNode;
            Assert.IsNotNull(testTopLevelKeyword1, "testTopLevelKeyword1");
            TaxonomyNode testKeyword11 = testTopLevelKeyword1.Items.FirstOrDefault(i => i.Title == TestFixture.Keyword1_1Title) as TaxonomyNode;
            Assert.IsNotNull(testKeyword11, "testKeyword11");
            PageModel testPageModel = SiteConfiguration.ContentProvider.GetPageModel(TestFixture.TaxonomyTestPage1UrlPath, testLocalization);
            string testPageSitemapItemId = string.Format("{0}-p{1}", testTaxonomyRoot.Id, testPageModel.Id);
            NavigationFilter testNavFilter = new NavigationFilter { IncludeAncestors = true, DescendantLevels = 1 };

            SitemapItem[] ancestorItems = _testOnDemandNavigationProvider.GetNavigationSubtree(testPageSitemapItemId, testNavFilter, testLocalization).ToArray();
            Assert.IsNotNull(ancestorItems, "ancestorItems");
            OutputJson(ancestorItems);

            // Result should be the Taxonomy Root only; it acts as the subtree root for all ancestors.
            Assert.AreEqual(1, ancestorItems.Length, "ancestorItems.Length");
            TaxonomyNode taxonomyRoot = ancestorItems[0] as TaxonomyNode;
            AssertExpectedTaxonomyNode(taxonomyRoot, testTaxonomyRoot.Title, 2, "taxonomyRoot");
            TaxonomyNode topLevelKeyword1 = taxonomyRoot.Items[0] as TaxonomyNode;
            AssertExpectedTaxonomyNode(topLevelKeyword1, testTopLevelKeyword1.Title, 2, "topLevelKeyword1");
            TaxonomyNode keyword11 = topLevelKeyword1.Items[0] as TaxonomyNode;
            AssertExpectedTaxonomyNode(keyword11, testKeyword11.Title, 2, "keyword11");
            TaxonomyNode keyword112 = keyword11.Items[0] as TaxonomyNode;
            AssertExpectedTaxonomyNode(keyword112, "Keyword 1.1.2", 1, "keyword112");
            TaxonomyNode keyword12 = topLevelKeyword1.Items[1] as TaxonomyNode;
            AssertExpectedTaxonomyNode(keyword12, "Keyword 1.2", 3, "keyword12");
            TaxonomyNode topLevelKeyword2 = taxonomyRoot.Items[1] as TaxonomyNode;
            AssertExpectedTaxonomyNode(topLevelKeyword2, TestFixture.TopLevelKeyword2Title, 2, "topLevelKeyword2");

            // Assert that child nodes are added because of DescendantLevels = 1:
            TaxonomyNode keyword111 = keyword11.Items[1] as TaxonomyNode;
            AssertExpectedTaxonomyNode(keyword111, "Keyword 1.1.1", -1, "keyword111");
        }


        [TestMethod]
        public void GetNavigationSubtree_IncludeAncestorsUnclassifiedPage_Success()
        {
            Localization testLocalization = TestFixture.ParentLocalization;
            TaxonomyNode testTaxonomyRoot = GetTestTaxonomy();
            PageModel testPageModel = SiteConfiguration.ContentProvider.GetPageModel(TestFixture.ArticlePageUrlPath, testLocalization);
            string testPageSitemapItemId = string.Format("{0}-p{1}", testTaxonomyRoot.Id, testPageModel.Id);
            NavigationFilter testNavFilter = new NavigationFilter { IncludeAncestors = true, DescendantLevels = 0 };

            SitemapItem[] ancestorItems = _testOnDemandNavigationProvider.GetNavigationSubtree(testPageSitemapItemId, testNavFilter, testLocalization).ToArray();
            Assert.IsNotNull(ancestorItems, "ancestorItems");
            OutputJson(ancestorItems);

            Assert.AreEqual(0, ancestorItems.Length, "ancestorItems.Length");
        }

        private static TaxonomyNode GetTestTaxonomy(IEnumerable<SitemapItem> taxonomyRoots = null, int descendantLevels = 1)
        {
            if (taxonomyRoots == null)
            {
                NavigationFilter navFilter = new NavigationFilter { DescendantLevels = descendantLevels };
                taxonomyRoots = _testOnDemandNavigationProvider.GetNavigationSubtree(null, navFilter, TestFixture.ParentLocalization);
            }

            TaxonomyNode result = taxonomyRoots.FirstOrDefault(tn => tn.Title == TestFixture.NavigationTaxonomyTitle) as TaxonomyNode;
            Assert.IsNotNull(result, "Test Taxonomy not found: " + TestFixture.NavigationTaxonomyTitle);
            return result;
        }

        private static void AssertNoChildItems(IEnumerable<SitemapItem> sitemapItems, string subjectName)
        {
            Assert.IsFalse(sitemapItems.Any(tn => tn.Items != null), subjectName + " has child items");
        }

    }
}
