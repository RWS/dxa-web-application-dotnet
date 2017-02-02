using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    }
}
