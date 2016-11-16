using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Tridion.Caching;

namespace Sdl.Web.Tridion.Tests
{
    public abstract class CacheProviderTest : TestClass
    {
        private const string TestKey1 = "TestKey1";
        private const string TestKey2 = "TestKey2";

        private readonly ICacheProvider _testCacheProvider;

        protected CacheProviderTest(ICacheProvider cacheProvider)
        {
            _testCacheProvider = cacheProvider;
        }

        [TestMethod]
        public void TryGetStore_IntValue_Success()
        {
            const int testValue1 = 666;
            const int testValue2 = 999;
            const string testRegion1 = "TryGetStore_IntValue_Success_1";
            const string testRegion2 = "TryGetStore_IntValue_Success_2";

            int cachedValue;
            Assert.IsFalse(_testCacheProvider.TryGet(TestKey1, testRegion1, out cachedValue));
            Assert.IsFalse(_testCacheProvider.TryGet(TestKey2, testRegion1, out cachedValue));
            Assert.IsFalse(_testCacheProvider.TryGet(TestKey1, testRegion2, out cachedValue));

            _testCacheProvider.Store(TestKey1, testRegion1, testValue1);
            _testCacheProvider.Store(TestKey2, testRegion1, testValue2);

            Assert.IsTrue(_testCacheProvider.TryGet(TestKey1, testRegion1, out cachedValue));
            Assert.AreEqual(testValue1, cachedValue);
            Assert.IsTrue(_testCacheProvider.TryGet(TestKey2, testRegion1, out cachedValue));
            Assert.AreEqual(testValue2, cachedValue);
            Assert.IsFalse(_testCacheProvider.TryGet(TestKey1, testRegion2, out cachedValue));

            _testCacheProvider.Store(TestKey1, testRegion1, testValue2);

            Assert.IsTrue(_testCacheProvider.TryGet(TestKey1, testRegion1, out cachedValue));
            Assert.AreEqual(testValue2, cachedValue);
        }

        [TestMethod]
        public void Store_Null_Success()
        {
            const string testRegion = "Store_Null_Success";

            _testCacheProvider.Store(TestKey1, testRegion, this);

            object cachedValue;
            Assert.IsTrue(_testCacheProvider.TryGet(TestKey1, testRegion, out cachedValue));
            Assert.AreEqual(this, cachedValue, "cachedValue");

            _testCacheProvider.Store(TestKey1, testRegion, (CacheProviderTest) null);
            Assert.IsFalse(_testCacheProvider.TryGet(TestKey1, testRegion, out cachedValue));
        }

        [TestMethod]
        public void GetOrAdd_IntValue_Success()
        {
            const int testValue = 666;
            const string testRegion = "GetOrAdd_IntValue_Success";

            int cachedValue = _testCacheProvider.GetOrAdd(TestKey1, testRegion, () => { Thread.Sleep(100); return testValue; });
            Assert.AreEqual(testValue, cachedValue);

            cachedValue = _testCacheProvider.GetOrAdd(TestKey1, testRegion, () => { Assert.Fail("Add function called second time."); return 0; });
            Assert.AreEqual(testValue, cachedValue);
        }

        [TestMethod]
        public void GetOrAdd_RaceCondition_Success()
        {
            const string testRegion = "GetOrAdd_RaceCondition_Success";
            int addFunctionCaller = 0;

            Parallel.For(
                1,
                10,
                i =>
                {
                    Console.WriteLine("Iteration {0}. Thread {1}.", i, Thread.CurrentThread.ManagedThreadId);
                    int cachedValue = _testCacheProvider.GetOrAdd(
                        TestKey1, 
                        testRegion,
                        () =>
                        {
                            // This should only be called once.
                            Console.WriteLine("Add function called by iteration {0}. Thread {1}", i, Thread.CurrentThread.ManagedThreadId);
                            Assert.AreEqual(0, addFunctionCaller, "addFunctionCaller");
                            addFunctionCaller = i;
                            Thread.Sleep(100);
                            return i;
                        }
                        );
                    // By this time the add function should have been called
                    Assert.AreNotEqual(0, addFunctionCaller, "addFunctionCaller");
                    Assert.AreEqual(addFunctionCaller, cachedValue, "cachedValue");
                }
                );
        }


        [TestMethod]
        public void TryGet_WrongType_Exception()
        {
            const string testRegion = "TryGet_WrongType_Exception";

            _testCacheProvider.Store(TestKey1, testRegion, 666);

            string cachedValue;
            AssertThrowsException<DxaException>(() => { _testCacheProvider.TryGet(TestKey1, testRegion, out cachedValue); }, "TryGet");
        }
    }
}
