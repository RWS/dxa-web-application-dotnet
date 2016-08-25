using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common.Interfaces;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
    public class CacheProviderTest : TestClass
    {
        private static readonly ICacheProvider _testCacheProvider = new DefaultCacheProvider();

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }

        [TestMethod]
        public void TryGetStore_IntValue_Success()
        {
            const int testValue1 = 666;
            const int testValue2 = 999;
            const string testKey1 = "TestKey1";
            const string testKey2 = "TestKey2";
            const string testRegion1 = "TryGetStore_IntValue_Success_1";
            const string testRegion2 = "TryGetStore_IntValue_Success_2";

            int cachedValue;
            Assert.IsFalse(_testCacheProvider.TryGet(testKey1, testRegion1, out cachedValue));
            Assert.IsFalse(_testCacheProvider.TryGet(testKey2, testRegion1, out cachedValue));
            Assert.IsFalse(_testCacheProvider.TryGet(testKey1, testRegion2, out cachedValue));

            _testCacheProvider.Store(testKey1, testRegion1, testValue1);
            _testCacheProvider.Store(testKey2, testRegion1, testValue2);

            Assert.IsTrue(_testCacheProvider.TryGet(testKey1, testRegion1, out cachedValue));
            Assert.AreEqual(testValue1, cachedValue);
            Assert.IsTrue(_testCacheProvider.TryGet(testKey2, testRegion1, out cachedValue));
            Assert.AreEqual(testValue2, cachedValue);
            Assert.IsFalse(_testCacheProvider.TryGet(testKey1, testRegion2, out cachedValue));

            _testCacheProvider.Store(testKey1, testRegion1, testValue2);

            Assert.IsTrue(_testCacheProvider.TryGet(testKey1, testRegion1, out cachedValue));
            Assert.AreEqual(testValue2, cachedValue);
        }

        [TestMethod]
        public void GetOrAdd_IntValue_Success()
        {
            const int testValue = 666;
            const string testKey1 = "TestKey1";
            const string testRegion1 = "GetOrAdd_IntValue_Success";

            int cachedValue = _testCacheProvider.GetOrAdd(testKey1, testRegion1, () => { Thread.Sleep(1000); return testValue; });
            Assert.AreEqual(testValue, cachedValue);

            cachedValue = _testCacheProvider.GetOrAdd(testKey1, testRegion1, () => { Assert.Fail("Add function called second time."); return 0; });
            Assert.AreEqual(testValue, cachedValue);
        }
    }
}
