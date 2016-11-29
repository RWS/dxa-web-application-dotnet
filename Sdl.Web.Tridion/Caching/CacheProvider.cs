using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;

namespace Sdl.Web.Tridion.Caching
{
    /// <summary>
    /// Abstract base class for DXA Cache Providers
    /// </summary>
    public abstract class CacheProvider : ICacheProvider
    {
        private const int WaitForAddingTimeout = 15000; // ms

        private static readonly IDictionary<string, EventWaitHandle> _addingEvents = new ConcurrentDictionary<string, EventWaitHandle>();

        #region ICacheProvider members
        public abstract void Store<T>(string key, string region, T value, IEnumerable<string> dependencies = null);

        public abstract bool TryGet<T>(string key, string region, out T value);

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
        public virtual T GetOrAdd<T>(string key, string region, Func<T> addFunction, IEnumerable<string> dependencies = null)
        {
            T result;

            if (TryGet(key, region, out result))
            {
                return result;
            }

            // Indicate to other threads that we're adding a value
            EventWaitHandle addingEvent = new ManualResetEvent(false);
            string qualifiedKey = GetQualifiedKey(key, region);
            try
            {
                _addingEvents.Add(qualifiedKey, addingEvent);
            }
            catch (ArgumentException)
            {
                // Another thread is already adding a value (race condition); retry getting the value.
                if (TryGet(key, region, out result))
                {
                    return result;
                }
            }

            try
            {
                // Obtain the actual value. This may be time-consuming (otherwise there would be no reason to cache).
                result = addFunction();

                Store(key, region, result, dependencies);
            }
            finally
            {
                // Unblock other threads which are awaiting it.
                addingEvent.Set();
                _addingEvents.Remove(qualifiedKey);
            }

            return result;
        }
        #endregion

        protected bool AwaitAddingValue(string key, string region)
        {
            string qualifiedKey = GetQualifiedKey(key, region);

            // Check if another thread is adding a value for the key/region:
            EventWaitHandle addingEvent;
            if (!_addingEvents.TryGetValue(qualifiedKey, out addingEvent))
            {
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

            return true;
        }

        protected static string GetQualifiedKey(string key, string region)
        {
            return string.Format("{0}::{1}", region, key);
        }
    }
}
