using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Tridion.ModelService;

namespace Sdl.Web.Tridion.Tests
{
    /// <summary>
    /// Unit/integration tests for the <see cref="DynamicNavigationProvider"/> based on legacy DXA Model Service.
    /// </summary>
    [TestClass]
    public class LegacyDynamicNavigationProviderTest : DynamicNavigationProviderTest
    {
        [ClassInitialize]
        public new static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext, typeof(DefaultModelServiceProvider));
        }

    }
}
