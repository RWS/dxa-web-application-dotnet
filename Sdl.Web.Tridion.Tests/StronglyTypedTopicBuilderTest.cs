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
        public void BuildEntityModel_NoMatchingStronglyTypedTopic_Success()
        {
            Topic genericTopic = new Topic
            {
                TopicBody = null
            };

            EntityModel testEntityModel = genericTopic;
            _testModelBuilder.BuildEntityModel(ref testEntityModel, null, null, TestFixture.ParentLocalization);

            Assert.AreSame(genericTopic, testEntityModel, "testEntityModel");
        }

        [TestMethod]
        public void BuildEntityModel_TopicBodyIllFormedXml_Success()
        {
            Topic genericTopic = new Topic
            {
                TopicBody = "<IllFormedXML>"
            };

            EntityModel testEntityModel = genericTopic;
            _testModelBuilder.BuildEntityModel(ref testEntityModel, null, null, TestFixture.ParentLocalization);

            Assert.AreSame(genericTopic, testEntityModel, "testEntityModel");
        }

        [TestMethod]
        public void BuildEntityModel_TitleBodySections_Success()
        {
            string testTitle = "DITA title";
            string testBody = "<div class=\"section \">First section</div><div class=\"section \">Second section</div>";
            Topic genericTopic = new Topic
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
            Assert.AreEqual("First sectionSecond section", result.Body, "result.Body"); // HTML tags should get stripped ("InnerText")
            Assert.IsNotNull(result.BodyRichText, "result.BodyRichText");
            Assert.AreEqual(testBody, result.BodyRichText.ToString(), "result.BodyRichText.ToString()");
            Assert.AreEqual("First section", result.FirstSection, "result.FirstSection");
            Assert.IsNotNull(result.Sections, "result.Sections");
            Assert.AreEqual(2, result.Sections.Count, "result.Sections.Count");
            Assert.AreEqual(result.FirstSection, result.Sections[0], "result.Sections[0]");
            Assert.IsNull(result.Links, "result.Links");
            Assert.IsNull(result.FirstChildLink, "result.FirstChildLink");
            Assert.IsNull(result.ChildLinks, "result.ChildLinks");
            Assert.IsNotNull(result.MvcData, "result.MvcData");
            Assert.AreEqual(result.GetDefaultView(null), result.MvcData, "result.MvcData");
        }

        [TestMethod]
        public void TryConvertToStronglyTypedTopic_Links_Success()
        {
            Topic genericTopic = new Topic
            {
                TopicBody = "<div class=\"body \" /><div class=\"related-links \">" +
                    "<div class=\"childlink \"><strong><a class=\"link \" href=\"/firstlink.html\">First link text</a></strong></div>" +
                    "<div class=\"childlink \"><strong><a class=\"link \" href=\"/secondlink.html\">Second link text</a></strong></div>" +
                    "<div class=\"parentlink \"><strong><a class=\"link \" href=\"/thirdlink.html\">Third link text</a></strong></div>" +
                    "</div>"
            };

            TestStronglyTypedTopic result = _testModelBuilder.TryConvertToStronglyTypedTopic<TestStronglyTypedTopic>(genericTopic);
            Assert.IsNotNull(result, "result");

            OutputJson(result);

            Assert.IsNull(result.Title, "result.Title");
            Assert.AreEqual(string.Empty, result.Body, "result.Body");
            Assert.IsNotNull(result.Links, "result.Links");
            Assert.AreEqual(3, result.Links.Count, "result.Links.Count");
            Assert.IsNotNull(result.FirstChildLink, "result.FirstChildLink");
            Assert.AreEqual("/firstlink.html", result.FirstChildLink.Url, "result.FirstChildLink.Url");
            Assert.AreEqual("First link text", result.FirstChildLink.LinkText, "result.FirstChildLink.LinkText");
            Assert.IsNull(result.FirstChildLink.AlternateText, "result.FirstChildLink.AlternateText");
            Assert.IsNotNull(result.ChildLinks, "result.ChildLinks");
            Assert.AreEqual(2, result.ChildLinks.Count, "result.ChildLinks.Count");
        }

        [TestMethod]
        public void TryConvertToStronglyTypedTopic_SpecializedTopic_Success()
        {
            string testTitle = "DITA title";
            string testBody = "<div class=\"section lcIntro \" id=\"s1\">Intro section</div><div class=\"section lcObjectives \" id=\"s2\">Objectives section</div>";
            Topic genericTopic = new Topic
            {
                TopicTitle = "Specialized topic title",
                TopicBody = $"<h1 class=\"title \">{testTitle}</h1><div class=\"body lcBaseBody lcOverviewBody \" id=\"b1\">{testBody}</div>"
            };

            TestSpecializedTopic result = _testModelBuilder.TryConvertToStronglyTypedTopic(genericTopic) as TestSpecializedTopic;
            Assert.IsNotNull(result, "result");

            OutputJson(result);

            Assert.IsNotNull(result.Intro, "result.Intro");
            Assert.IsNotNull(result.Objectives, "result.Objectives");
            Assert.IsNotNull(result.Body, "result.Body");
            Assert.IsNotNull(result.Body.Intro, "result.Body.Intro");
            Assert.IsNotNull(result.Body.Objectives, "result.Body.Objectives");
            Assert.IsNotNull(result.Body.Objectives.Content, "result.Body.Objectives.Content");
            Assert.IsNotNull(result.Body.Sections, "result.Body.Sections");

            Assert.AreEqual("Intro section", result.Intro.ToString(), "result.Intro.ToString()");
            Assert.AreEqual("Objectives section", result.Objectives.ToString(), "result.Objectives.ToString()");

            Assert.AreEqual("b1", result.Body.Id, "result.Body.Id");
            Assert.AreEqual("body lcBaseBody lcOverviewBody ", result.Body.HtmlClasses, "body lcBaseBody lcOverviewBody ");
            Assert.AreEqual("Intro section", result.Body.Intro.ToString(), "result.Body.Intro.ToString()");

            Assert.AreEqual("s2", result.Body.Objectives.Id, "result.Body.Objectives.Id");
            Assert.AreEqual("section lcObjectives ", result.Body.Objectives.HtmlClasses, "result.Body.Objectives.HtmlClasses");
            Assert.AreEqual("Objectives section", result.Body.Objectives.Content.ToString(), "result.Body.Objectives.Content.ToString()");

            Assert.AreEqual(2, result.Body.Sections.Count, "result.Body.Sections.Count");
            Assert.AreEqual("s1", result.Body.Sections[0].Id, "result.Body.Sections[0].Id");
            Assert.AreEqual("s2", result.Body.Sections[1].Id, "result.Body.Sections[1].Id");
            Assert.AreEqual("section lcIntro ", result.Body.Sections[0].HtmlClasses, "result.Body.Sections[0].HtmlClasses");
            Assert.AreEqual("section lcObjectives ", result.Body.Sections[1].HtmlClasses, "result.Body.Sections[1].HtmlClasses");

            Assert.IsNotNull(result.MvcData, "result.MvcData");
            Assert.AreEqual(result.GetDefaultView(null), result.MvcData, "result.MvcData");
            Assert.IsNotNull(result.Body.MvcData, "result.Body.MvcData");
            Assert.AreEqual(result.Body.GetDefaultView(null), result.Body.MvcData, "result.Body.MvcData");

        }

    }

}
