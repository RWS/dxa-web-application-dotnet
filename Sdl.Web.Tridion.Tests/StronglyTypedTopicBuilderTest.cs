using Sdl.Web.Common.Models;
using Sdl.Web.Tridion.Mapping;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common.Models.Entity;
using Sdl.Web.Common;
using Sdl.Web.Tridion.Tests.Models.Topic;
using Sdl.Web.Tridion.Tests.Models;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class StronglyTypedTopicBuilderTest : TestClass
    {
        StronglyTypedTopicBuilder _testModelBuilder = new StronglyTypedTopicBuilder();

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }

        [TestMethod]
        public void BuildEntityModel_Null_Exception()
        {
            // This would happen if you accidentally configure this model builder as the first in the pipeline.
            EntityModel testEntityModel = null;
            AssertThrowsException<DxaException>(() =>  _testModelBuilder.BuildEntityModel(ref testEntityModel, null, null, TestFixture.ParentLocalization));
        }


        [TestMethod]
        public void BuildEntityModel_NotTopic_Success()
        {
            Article testArticle = new Article();

            EntityModel testEntityModel = testArticle;
            _testModelBuilder.BuildEntityModel(ref testEntityModel, null, null, TestFixture.ParentLocalization);

            Assert.AreSame(testArticle, testEntityModel, "testEntityModel");
        }


        [TestMethod]
        public void BuildEntityModel_TitleBodySections_Success()
        {
            string testTitle = "DITA title";
            string testBody = "<div class=\"section \">First section</div><div class=\"section \">Second section</div>";
            Topic genericTopic = new Topic()
            {
                TopicTitle = "Test topic title",
                TopicBody = $"<h1 class=\"title \">{testTitle}</h1><div class=\"body \">{testBody}</div>"
            };

            EntityModel testEntityModel = genericTopic;
            _testModelBuilder.BuildEntityModel(ref testEntityModel, null, null, TestFixture.ParentLocalization);

            OutputJson(testEntityModel);

            TestStronglyTypedTopic result = testEntityModel as TestStronglyTypedTopic;
            Assert.IsNotNull(result, "result");
            Assert.AreEqual(testTitle, result.Title, "result.Title");
            Assert.AreEqual(testBody, result.Body, "result.Body");
            Assert.AreEqual("First section", result.FirstSection, "result.FirstSection");
            Assert.IsNotNull(result.Sections, "result.Sections");
            Assert.IsNull(result.FirstLink, "result.FirstLink");
            Assert.IsNull(result.Links, "result.Links");
        }

        [TestMethod]
        public void BuildStronglyTypedTopic_Links_Success()
        {
            Topic genericTopic = new Topic()
            {
                TopicBody = "<div class=\"body \"></div><div class=\"related-links \">" +
                    "<div class=\"childlink \"><strong><a class=\"link \" href=\"/firstlink.html\">First link text</a></strong></div>" +
                    "<div class=\"childlink \"><strong><a class=\"link \" href=\"/secondlink.html\">Second link text</a></strong></div>" +
                    "</div>"
            };

            TestStronglyTypedTopic result = _testModelBuilder.BuildStronglyTypedTopic(genericTopic) as TestStronglyTypedTopic;
            Assert.IsNotNull(result, "testStronglyTypedTopic");

            OutputJson(result);

            Assert.IsNull(result.Title, "result.Title");
            Assert.AreEqual(string.Empty, result.Body, "result.Body");
            Assert.IsNotNull(result.FirstLink, "result.FirstLink");
            Assert.AreEqual("/firstlink.html", result.FirstLink.Url, "result.FirstLink.Url");
            Assert.AreEqual("First link text", result.FirstLink.LinkText, "result.FirstLink.LinkText");
            Assert.IsNull(result.FirstLink.AlternateText, "result.FirstLink.AlternateText");
            Assert.IsNotNull(result.Links, "result.Links");
            Assert.AreEqual(2, result.Links.Count, "result.Links.Count");
        }

    }

}
