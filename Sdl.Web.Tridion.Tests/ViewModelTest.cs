using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
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

        [TestMethod]
        public void ExtensionData_Success()
        {
            const string testExtensionDataKey = "TestExtensionData";
            const int testExtensionDataValue = 666;
            ViewModel testViewModel = new PageModel("666");

            Assert.IsNull(testViewModel.ExtensionData, "testViewModel.ExtensionData (before)");

            testViewModel.SetExtensionData(testExtensionDataKey, testExtensionDataValue);
            OutputJson(testViewModel);

            Assert.IsNotNull(testViewModel.ExtensionData, "testViewModel.ExtensionData");
            Assert.AreEqual(1, testViewModel.ExtensionData.Count, "testViewModel.ExtensionData.Count");
            Assert.AreEqual(testExtensionDataValue, testViewModel.ExtensionData[testExtensionDataKey], "testViewModel.ExtensionData[testExtensionDataKey]");
        }

        [TestMethod]
        public void ExtractSyndicationFeedItems_Teasers_Success()
        {
            ILocalization testLocalization = TestFixture.ParentLocalization;
            Teaser testTeaser1 = new Teaser
            {
                Headline = "Test Teaser 1",
                Text = new RichText("This is the text of Test Teaser 1."),
                Link = new Link() { Url = "http://www.sdl.com/" }
            };
            Teaser testTeaser2 = new Teaser
            {
                Headline = "Test Teaser 2",
            };
            RegionModel testRegion1 = new RegionModel("test1");
            testRegion1.Entities.Add(testTeaser1);
            RegionModel testRegion2 = new RegionModel("test2");
            testRegion2.Entities.Add(testTeaser2);
            PageModel testPageModel = new PageModel("666");
            testPageModel.Regions.Add(testRegion1);
            testPageModel.Regions.Add(testRegion2);
            OutputJson(testPageModel);

            SyndicationItem[] syndicationItems = testPageModel.ExtractSyndicationFeedItems(testLocalization).ToArray();

            Assert.IsNotNull(syndicationItems);
            Assert.AreEqual(2, syndicationItems.Length, "syndicationItems.Length");

            SyndicationItem firstSyndicationItem = syndicationItems[0];
            Assert.IsNotNull(firstSyndicationItem, "firstSyndicationItem");
            Assert.IsNotNull(firstSyndicationItem.Title, "firstSyndicationItem.Title");
            Assert.IsNotNull(firstSyndicationItem.Summary, "firstSyndicationItem.Summary");
            Assert.IsNotNull(firstSyndicationItem.Links, "firstSyndicationItem.Links");
            Assert.AreEqual(testTeaser1.Headline, firstSyndicationItem.Title.Text, "firstSyndicationItem.Title.Text");
            Assert.AreEqual(testTeaser1.Text.ToString(), firstSyndicationItem.Summary.Text, "firstSyndicationItem.Summary.Text");
            Assert.AreEqual(1, firstSyndicationItem.Links.Count, "firstSyndicationItem.Links.Count");
            Assert.AreEqual(testTeaser1.Link.Url, firstSyndicationItem.Links[0].Uri.ToString(), "firstSyndicationItem.Links[0].Uri");

            SyndicationItem secondSyndicationItem = syndicationItems[1];
            Assert.IsNotNull(secondSyndicationItem, "secondSyndicationItem");
            Assert.IsNotNull(secondSyndicationItem.Title, "secondSyndicationItem.Title");
            Assert.IsNull(secondSyndicationItem.Summary, "secondSyndicationItem.Summary");
            Assert.IsNotNull(secondSyndicationItem.Links, "secondSyndicationItem.Links");
            Assert.AreEqual(testTeaser2.Headline, secondSyndicationItem.Title.Text, "secondSyndicationItem.Title.Text");
            Assert.AreEqual(0, secondSyndicationItem.Links.Count, "secondSyndicationItem.Links.Count");
        }

        [TestMethod]
        public void ExtractSyndicationFeedItems_None_Success()
        {
            ILocalization testLocalization = TestFixture.ParentLocalization;
            PageModel testPageModel = new PageModel("666");
            OutputJson(testPageModel);

            SyndicationItem[] syndicationItems = testPageModel.ExtractSyndicationFeedItems(testLocalization).ToArray();

            Assert.IsNotNull(syndicationItems);
            Assert.AreEqual(0, syndicationItems.Length, "syndicationItems.Length");
        }


        [TestMethod]
        public void ToHtml_Image_Success()
        {
            Image imageWithoutUrl = new Image();
            Image imageWithUrl = new Image() { Url = "http://www.dummy.org/fakeimage.png", AlternateText = "Fake Image"};

            MockMediaHelper.ResponsiveImageRequests.Clear();

            string imageWithoutUrlHtml = imageWithoutUrl.ToHtml();
            string imageWithUrlHtml = imageWithUrl.ToHtml();

            Assert.IsNotNull(imageWithoutUrlHtml, "imageWithoutUrlHtml");
            Assert.IsNotNull(imageWithUrlHtml, "imageWithUrlHtml");
            Console.WriteLine(imageWithoutUrlHtml);
            Console.WriteLine(imageWithUrlHtml);

            Assert.AreEqual(0, imageWithoutUrlHtml.Length, "imageWithoutUrlHtml.Length");
            Assert.AreEqual(1, MockMediaHelper.ResponsiveImageRequests.Count, "ResponsiveImageRequests.Count");
            string responsiveImageUrl = MockMediaHelper.ResponsiveImageRequests[0].ResponsiveImageUrl;
            string expectedImgHtml = string.Format("<img src=\"{0}\" alt=\"{1}\" data-aspect=\"0\" width=\"100%\"/>", responsiveImageUrl, imageWithUrl.AlternateText);
            Assert.AreEqual(expectedImgHtml, imageWithUrlHtml, "imageWithUrlHtml");
        }

        [TestMethod]
        public void ToHtml_Article_Exception()
        {
            Article testViewModel = new Article();

            AssertThrowsException<NotSupportedException>(() => { testViewModel.ToHtml(); });
        }
    }
}
