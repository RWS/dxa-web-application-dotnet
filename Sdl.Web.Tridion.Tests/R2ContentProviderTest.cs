using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using Sdl.Web.Tridion.Tests.Models;
using Sdl.Web.DataModel;
using Sdl.Web.DataModel.Extension;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class R2ContentProviderTest : ContentProviderTest
    {
        public R2ContentProviderTest()
            : base(new Mapping.GraphQLContentProvider(), () => TestFixture.ParentLocalization)
        {
        }

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }

        [TestMethod]
        public void GetPageModel_ContextExpressions_Success()
        {
            string testPageUrlPath = TestLocalization.GetAbsoluteUrlPath(TestFixture.ContextExpressionsTestPageRelativeUrlPath);

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            OutputJson(pageModel);

            RegionModel mainRegion = pageModel.Regions["Main"];
            EntityModel[] entitiesWithExtensionData =
                mainRegion.Entities.Where(
                    e => e.ExtensionData != null).ToArray();

            int numIncludes = 0;
            int numExcludes = 0;
            foreach (ContentModelData contextExpressions in from entity 
                    in entitiesWithExtensionData
                    where entity.ExtensionData.ContainsKey("ContextExpressions")
                    select (ContentModelData) entity.ExtensionData["ContextExpressions"])
            {
                if (contextExpressions.ContainsKey("Include"))
                {
                    if (contextExpressions["Include"] is string)
                        numIncludes++;
                    if (contextExpressions["Include"] is string[])
                        numIncludes += ((string[]) contextExpressions["Include"]).Length;
                }
                if (contextExpressions.ContainsKey("Exclude"))
                {
                    if (contextExpressions["Exclude"] is string)
                        numExcludes++;
                    if (contextExpressions["Exclude"] is string[])
                        numExcludes += ((string[])contextExpressions["Exclude"]).Length;
                }
            }

            Assert.AreEqual(8, entitiesWithExtensionData.Length, "entitiesWithExtensionData.Length");
            Assert.AreEqual(8, numIncludes, "numIncludes");
            Assert.AreEqual(4, numExcludes, "numExcludes");
        }

        [TestMethod]
        public void GetPageModel_RetrofitMapping_Success() // See TSI-1757
        {
            ILocalization testLocalization = TestFixture.ChildLocalization;
            string testPageUrlPath = testLocalization.GetAbsoluteUrlPath(TestFixture.Tsi1757PageRelativeUrlPath);

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, testLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            OutputJson(pageModel);

            Tsi1757TestEntity3 testEntity3 = pageModel.Regions["Main"].Entities[0] as Tsi1757TestEntity3;
            Assert.IsNotNull(testEntity3, "testEntity3");

            Assert.AreEqual("This is the textField of TSI-1757 Test Component 3", testEntity3.TextField, "testEntity3.TextField");
            Assert.IsNotNull(testEntity3.CompLinkField, "testEntity3.CompLinkField");
            Assert.AreEqual(2, testEntity3.CompLinkField.Count, "testEntity3.CompLinkField.Count");

            Tsi1757TestEntity1 testEntity1 = testEntity3.CompLinkField[0] as Tsi1757TestEntity1;
            Assert.IsNotNull(testEntity1, "testEntity1");
            Assert.AreEqual("This is the textField of TSI-1757 Test Component 1", testEntity1.TextField, "testEntity1.TextField");
            Assert.AreEqual("This is the embeddedTextField of TSI-1757 Test Component 1", testEntity1.EmbeddedTextField, "testEntity1.EmbeddedTextField");

            Tsi1757TestEntity2 testEntity2 = testEntity3.CompLinkField[1] as Tsi1757TestEntity2;
            Assert.IsNotNull(testEntity2, "testEntity2");
            Assert.AreEqual("This is the textField of TSI-1757 Test Component 2", testEntity2.TextField, "testEntity2.TextField");
        }


        [TestMethod]
        public void GetPageModel_WithTargetGroupConditions_Success() // See TSI-2844, TSI-3010, TSI-3637
        {
            string testPageUrlPath = TestLocalization.GetAbsoluteUrlPath(TestFixture.Tsi3010PageRelativeUrlPath);

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            OutputJson(pageModel);

            Tsi3010TestEntity testEntity = pageModel.Regions["Main"].Entities[0] as Tsi3010TestEntity;

            Assert.IsNotNull(testEntity, "testEntity");
            Assert.IsNotNull(testEntity.ExtensionData, "testEntity.ExtensionData");
            Condition[] conditions = testEntity.ExtensionData["TargetGroupConditions"] as Condition[];
            Assert.IsNotNull(conditions, "conditions");
            Assert.AreEqual(3, conditions.Length, "conditions.Count");

            CustomerCharacteristicCondition ccCondition = conditions.OfType<CustomerCharacteristicCondition>().FirstOrDefault();
            Assert.IsNotNull(ccCondition, "ccCondition");
            Assert.AreEqual("Browser", ccCondition.Name, "ccCondition.Name");
            Assert.AreEqual("Chrome", ccCondition.Value, "ccCondition.Value");
            Assert.AreEqual(ConditionOperator.StringEquals, ccCondition.Operator, "ccCondition.Operator");
            Assert.AreEqual(false, ccCondition.Negate, "ccCondition.Negate");

            TrackingKeyCondition tkCondition = conditions.OfType<TrackingKeyCondition>().FirstOrDefault();
            Assert.IsNotNull(tkCondition, "tkCondition");
            Assert.AreEqual("Top-level Keyword 1", tkCondition.TrackingKeyTitle, "tkCondition.TrackingKeyTitle");
            Assert.AreEqual(3.0, tkCondition.Value, "tkCondition.Value");
            Assert.AreEqual(ConditionOperator.Equals, tkCondition.Operator, "tkCondition.Operator");
            Assert.AreEqual(true, tkCondition.Negate, "tkCondition.Negate");

            TargetGroupCondition tgCondition = conditions.OfType<TargetGroupCondition>().FirstOrDefault();
            Assert.IsNotNull(tgCondition, "tgCondition");
        }

        [TestMethod]
        public void GetPageModel_WithInheritedEntityMetadata_Success() // See TSI-2844, TSI-3723
        {
            string testPageUrlPath = TestLocalization.GetAbsoluteUrlPath(TestFixture.Tsi2844PageRelativeUrlPath);

            PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: false);

            Assert.IsNotNull(pageModel, "pageModel");
            OutputJson(pageModel);

            Tsi2844TestEntity testEntity = pageModel.Regions["Main"].Entities[0] as Tsi2844TestEntity;
            Assert.IsNotNull(testEntity, "testEntity");

            Assert.AreEqual("Tsi2844 TextValue XPM", testEntity.SingleLineText, "testEntity.SingleLineText"); // From Component Content
            Assert.AreEqual("Tsi2844 Metadata TextValue JAVA", testEntity.MetadataTextField, "testEntity.MetadataTextField"); // From Component Metadata
            Assert.AreEqual("TSI-2844 Folder Metadata Text Value", testEntity.FolderMetadataTextField, "testEntity.FolderMetadataTextField"); // From Folder Metadata

            // Traces of the use of Extension Data to convey the Schema IDs of the ancestor Metadata Schemas
            Assert.IsNotNull(testEntity.ExtensionData, "testEntity.ExtensionData");
            string[] schemas = testEntity.ExtensionData["Schemas"] as string[];
            Assert.IsNotNull(schemas, "schemas");
            Assert.AreEqual(1, schemas.Length, "schemas.Length");
        }

        [TestMethod]
        public void GetPageModel_WithInheritedPageMetadata_Success() // See TSI-2844, CRQ-12170
        {
            string testPageUrlPath = TestLocalization.GetAbsoluteUrlPath(TestFixture.Tsi2844Page2RelativeUrlPath);

            Tsi2844PageModel pageModel = TestContentProvider.GetPageModel(testPageUrlPath, TestLocalization, addIncludes: false) as Tsi2844PageModel;

            Assert.IsNotNull(pageModel, "pageModel");
            OutputJson(pageModel);

            Assert.AreEqual("TSI-2844 Structure Group Metadata", pageModel.FolderMetadataTextField, "pageModel.FolderMetadataTextField");

            // Traces of the use of Extension Data to convey the Schema IDs of the ancestor Metadata Schemas
            Assert.IsNotNull(pageModel.ExtensionData, "pageModel.ExtensionData");
            string[] schemas = pageModel.ExtensionData["Schemas"] as string[];
            Assert.IsNotNull(schemas, "schemas");
            Assert.AreEqual(1, schemas.Length, "schemas.Length");
        }

    }
}
