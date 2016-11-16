using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Tridion.Caching;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class DefaultCacheProviderTest : CacheProviderTest
    {
        public DefaultCacheProviderTest() : base(new DefaultCacheProvider())
        {
        }

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }
    }
}
