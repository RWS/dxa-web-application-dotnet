using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Tridion.Caching;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class DD4TCacheProviderTest : CacheProviderTest
    {
        public DD4TCacheProviderTest() : base(new DD4TCacheProvider())
        {
        }

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }
    }
}
