using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class ContentProviderTest : TestClass
    {
        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }

        [TestMethod]
        public void GetEntityModel_IsQueryBased_Success()
        {
            string testEntityId = TestFixture.ArticleDcpEntityId;

            EntityModel entityModel = SiteConfiguration.ContentProvider.GetEntityModel(testEntityId, TestFixture.ParentLocalization);

            Assert.IsNotNull(entityModel, "entityModel");
            Assert.AreEqual(entityModel.Id, testEntityId, "entityModel.Id");
            Assert.IsNotNull(entityModel.XpmMetadata, "entityModel.XpmMetadata");
            string isQueryBased;
            Assert.IsTrue(entityModel.XpmMetadata.TryGetValue("IsQueryBased", out isQueryBased), "XpmMetadata contains 'IsQueryBased'");
            Assert.AreEqual(isQueryBased, "true", "IsQueryBased value");
        }

        [TestMethod]
        [ExpectedException(typeof(DxaItemNotFoundException))]
        public void GetEntityModel_NonExistent_Exception()
        {
            const string testEntityId = "666-666"; // Should not actually exist
            SiteConfiguration.ContentProvider.GetEntityModel(testEntityId, TestFixture.ParentLocalization);
        }
    }
}
