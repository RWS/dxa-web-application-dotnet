using Sdl.Web.Common.Models;
using Sdl.Web.Tridion.Mapping;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common.Models.Entity;
using Sdl.Web.Common;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class StronglyTypedTopicBuilderTest : TestClass
    {
        IEntityModelBuilder _testModelBuilder = new StronglyTypedTopicBuilder();

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
        public void BuildEntityModel_Success()
        {
            Topic genericTopic = new Topic()
            {
                TopicTitle = "Test topic title",
                TopicBody = "<h1 class=\"title \">DITA title</h1><div class=\"body \">DITA body</div>"
            };

            EntityModel testEntityModel = genericTopic;
            _testModelBuilder.BuildEntityModel(ref testEntityModel, null, null, TestFixture.ParentLocalization);

            Assert.AreNotSame(genericTopic, testEntityModel, "Generic Topic is not transformed.");
        }

    }

}
