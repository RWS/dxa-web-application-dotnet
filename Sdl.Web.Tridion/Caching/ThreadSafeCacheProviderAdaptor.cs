using DD4T.ContentModel.Contracts.Caching;
using DD4T.Utils.Caching;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Tridion.Mapping;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sdl.Web.Tridion.Caching
{
    /// <summary>
    /// ICacheProvider Adapator implementation to provide thread safe caching on top of the DD4T cache agent
    /// </summary>
    internal class ThreadSafeCacheProviderAdaptor : ICacheProvider
    {
        private const int WaitForAddingTimeout = 15000; // ms
        private readonly ICacheAgent _cacheAgent = null;
        private static readonly IDictionary<string, EventWaitHandle> _addingEvents = new ConcurrentDictionary<string, EventWaitHandle>();

        public ThreadSafeCacheProviderAdaptor(ICacheAgent cacheAgent)
        {
            _cacheAgent = cacheAgent;
        }

        /// <summary>
        /// Stores a given key/value pair to a given cache Region.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The name of the cache region. Different cache regions can have different retention policies.</param>
        /// <param name="value">The value. If <c>null</c>, this effectively removes the key from the cache.</param>
        /// <param name="dependencies">An optional set of dependent item IDs. Can be used to invalidate the cached item.</param>
        /// <typeparam name="T">The type of the value to add.</typeparam>
        public void Store<T>(string key, string region, T value, IEnumerable<string> dependencies = null)
        {
            List<string> dependsOnTcmUris = (dependencies == null) ? null : dependencies.ToList();
            string cacheAgentKey = GetCacheAgentKey(key, region);
            lock (_cacheAgent)
            {
                _cacheAgent.Remove(cacheAgentKey); // DD4T doesn't overwrite existing values (?)
                if (value != null) // DD4T doesn't support storing null values
                {
                    _cacheAgent.Store(cacheAgentKey, region, value, dependsOnTcmUris);
                }
            }
        }

        /// <summary>
        /// Tries to get a cached value for a given key and cache region.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The name of the cache region. Different cache regions can have different retention policies.</param>
        /// <param name="value">The cached value (output).</param>
        /// <typeparam name="T">The type of the value to get.</typeparam>
        /// <returns><c>true</c> if a cached value was found for the given key and cache region.</returns>
        public bool TryGet<T>(string key, string region, out T value)
        {
            string cacheAgentKey = GetCacheAgentKey(key, region);
            object cachedValue = _cacheAgent.Load(cacheAgentKey);
            if (cachedValue == null)
            {
                // Check if another thread is adding a value for the key/region:
                EventWaitHandle addingEvent;
                if (!_addingEvents.TryGetValue(cacheAgentKey, out addingEvent))
                {
                    value = default(T);
                    return false;
                }

                Log.Debug("Awaiting adding of value for key '{0}' in region '{1}' ...", key, region);
                if (!addingEvent.WaitOne(WaitForAddingTimeout))
                {
                    // To facilitate diagnosis of deadlock conditions, first log a warning and then wait another timeout period.
                    Log.Warn("Waiting for adding of value for key '{0}' in cache region '{1}' for {2} seconds.", key, region, WaitForAddingTimeout / 1000);
                    if (!addingEvent.WaitOne(WaitForAddingTimeout))
                    {
                        throw new DxaException(string.Format("Timeout waiting for adding of value for key '{0}' in cache region '{1}'.", key, region));
                    }
                }
                Log.Debug("Done awaiting.");

                // After the event has been signalled, the value is cached. So, retry.
                cachedValue = _cacheAgent.Load(cacheAgentKey);
                if (cachedValue == null)
                {
                    value = default(T);
                    return false;
                }
            }

            if (!(cachedValue is T))
            {
                throw new DxaException(
                    string.Format("Cached value for key '{0}' in region '{1}' is of type {2} instead of {3}.", key, region, cachedValue.GetType().FullName, typeof(T).FullName)
                    );
            }

            value = (T) cachedValue;
            return true;
        }

        /// <summary>
        /// Tries to gets a value for a given key and cache region. If not found, add a value obtained from a given function.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The name of the cache region. Different cache regions can have different retention policies.</param>
        /// <param name="addFunction">A function (delegate) used to obtain the value to add in case an existing cached value is not found.</param>
        /// <param name="dependencies">An optional set of dependent item IDs. Can be used to invalidate the cached item.</param>
        /// <typeparam name="T">The type of the value to get or add.</typeparam>
        /// <remarks>
        /// This method is thread-safe; it prevents the same key being added by multiple threads in case of a race condition.
        /// </remarks>
        /// <returns>The cached value.</returns>
        public T GetOrAdd<T>(string key, string region, Func<T> addFunction, IEnumerable<string> dependencies = null)
        {
            T result;

            if (TryGet(key, region, out result))
            {
                return result;
            }

            // Indicate to other threads that we're adding a value
            EventWaitHandle addingEvent = new ManualResetEvent(false);
            string cacheAgentKey = GetCacheAgentKey(key, region);
            try
            {
                _addingEvents.Add(cacheAgentKey, addingEvent);
            }
            catch (ArgumentException)
            {
                // Another thread is already adding a value (race condition); retry getting the value.
                if (TryGet(key, region, out result))
                {
                    return result;
                }
            }

            // Obtain the actual value. This may be time-consuming (otherwise there would be no reason to cache).
            result = addFunction();

            // Cache the value and unblock other threads which are awaiting it.
            Store(key, region, result, dependencies);
            addingEvent.Set();
            _addingEvents.Remove(cacheAgentKey);

            return result;
        }

        private static string GetCacheAgentKey(string key, string region)
        {
            return string.Format("{0}::{1}", region, key);
        }
    }
}
