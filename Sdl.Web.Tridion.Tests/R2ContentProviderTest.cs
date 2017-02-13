using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class R2ContentProviderTest : ContentProviderTest
    {
        public R2ContentProviderTest()
            : base(new R2Mapping.DefaultContentProviderR2(), () => TestFixture.R2TestLocalization)
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
            EntityModel[] entitiesWithExtensionData = mainRegion.Entities.Where(e => e.ExtensionData != null).ToArray();
            EntityModel[] entitiesWithCxInclude = entitiesWithExtensionData.Where(e => e.ExtensionData.ContainsKey("CX.Include")).ToArray();
            EntityModel[] entitiesWithCxExclude = entitiesWithExtensionData.Where(e => e.ExtensionData.ContainsKey("CX.Exclude")).ToArray();

            Assert.AreEqual(8, entitiesWithExtensionData.Length, "entitiesWithExtensionData.Length");
            Assert.AreEqual(6, entitiesWithCxInclude.Length, "entitiesWithCxInclude.Length");
            Assert.AreEqual(4, entitiesWithCxExclude.Length, "entitiesWithCxExclude.Length");
        }
    }
}
