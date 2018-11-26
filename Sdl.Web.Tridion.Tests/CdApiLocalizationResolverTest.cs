using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class CdApiLocalizationResolverTest : LocalizationResolverTest
    {
        public CdApiLocalizationResolverTest() : base(new CdApiLocalizationResolver())
        {
        }

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }
    }
}
