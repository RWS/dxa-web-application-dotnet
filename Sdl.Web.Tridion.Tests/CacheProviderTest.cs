using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;

namespace Sdl.Web.Tridion.Tests
{
    [TestClass]
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

        //[TestMethod]
        // Currently we disable this test because we have changed how caching works. 
        // In the old scheme we would block threads until the first thread that came to generate the cache value 
        // finished and then signal waiting threads to then continue. 
        // This strategy is not very good in practise since it blocked progress and was difficult to reason about. 
        //
        // For example:
        //  You have to hope the scheduller doesn't suspend the thread doing the work for too long and that all
        //  waiting threads yield. If the thread doing the work is unlucky and not switched in by the scheduler
        //  then it will block all other threads and no progress will be made. Equally if its constantly 
        //  switched in and out, progress is very very slow.
        //
        // Now we just accept the fact that in a bad situation with mutliple threads accessing the exact same key 
        // for the first time we just take the hit and let each thread calculate the value. Only one thread 
        // (the first) will get to write the value to the cache but all others will at least make progress.
        // In a good situation you will not get lots of threads trying to calculate the same thing and the cache
        // value will exist because some thread at some point calculated it.
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

        [TestMethod]
        public void Store_Expiration_Success()
        {
            const string testRegion = "Store_Expiration_Success";
            const int timeout = 15;

            _testCacheProvider.Store(TestKey1, testRegion, 666);

            int cachedValue;
            Assert.IsTrue(_testCacheProvider.TryGet(TestKey1, testRegion, out cachedValue), "Value is not cached.");

            // Test that it's absolute expiration rather than sliding expiration by regularly accessing the cache key.
            for (int i = 1; i <= timeout; i++)
            {
                Thread.Sleep(1000);
                if (_testCacheProvider.TryGet(TestKey1, testRegion, out cachedValue))
                {
                    continue;
                }
                Console.WriteLine("Cache expired after {0} seconds.", i);
                return;
            }

            Assert.Fail("Value is still cached after {0} seconds.", timeout);
        }
    }
}
