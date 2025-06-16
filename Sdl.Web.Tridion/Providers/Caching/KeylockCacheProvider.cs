using AsyncKeyedLock;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Delivery.Caching;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sdl.Web.Tridion.Caching
{
    /// <summary>
    /// Key lock cache provider wraps CIL cache access with lock syncronisation.
    /// 
    /// 1. Prevents multiple threads doing the same heavy lifting:
    ///     Lock syncronisation is used so only the first thread to take the lock will generate the cache value. All 
    ///     subsequent threads will block until the first thread completes.
    ///    
    /// 2. Prevents each thread from getting deadlocked by its own locks (re-entrant caching):
    ///     The API provided by the ICacheProvider allows clients to pass a lamda function for calculating the
    ///     cache value. A potential issue with this mechanism is the lamda function could try to retrieve a value
    ///     from the cache that uses the same cache key (even a different key that has a hash collision may cause 
    ///     this). This can result in a deadlock. We resolve this issue by dealing with re-entrant lamda functions 
    ///     using a thread local atomic counter. When a lock is taken the lock object used is created based on the cache 
    ///     key and also this local atomic counter. 
    /// </summary>
    public class KeylockCacheProvider : ICacheProvider
    {
        private static readonly AsyncKeyedLocker<string> _asyncKeyedLocker = new AsyncKeyedLocker<string>(o =>
        {
            o.PoolSize = 20;
            o.PoolInitialFill = 1;
        });

        private readonly ICacheProvider<object> _cilCacheProvider = CacheFactory<object>.CreateFromConfiguration();

        public void Store<T>(string key, string region, T value, IEnumerable<string> dependencies = null)
        {
            Debug.Assert(_cilCacheProvider != null, "_cilCacheProvider != null");

            // prevent deadlocks if we're storing something here and in GetOrAdd.
            var hash = CalcLockHash(key, region);
            using (_asyncKeyedLocker.Lock(hash))
            {
                // only add if we are sure it hasn't already been added
                if (!TryGetCachedValue(key, region, out T cachedValue))
                {
                    _cilCacheProvider.Set(key, value, region);
                }
            }
        }

        public bool TryGet<T>(string key, string region, out T value) => TryGetCachedValue(key, region, out value);

        public T GetOrAdd<T>(string key, string region, Func<T> addFunction, IEnumerable<string> dependencies = null)
        {
            if (TryGetCachedValue(key, region, out T cachedValue)) return cachedValue;
            var hash = CalcLockHash(key, region);
            using (_asyncKeyedLocker.Lock(hash))
            {
                // Try and get from cache again in case it has been added in the meantime
                if (TryGetCachedValue(key, region, out cachedValue)) return cachedValue;

                // Still null, so lets run Func()
                cachedValue = addFunction();
                if (cachedValue != null)
                {
                    // Note that dependencies are not used?
                    Debug.Assert(_cilCacheProvider != null, "_cilCacheProvider != null");
                    if (_cilCacheProvider.Get(key, region) == null)
                    {
                        _cilCacheProvider.Set(key, cachedValue, region);
                    }

                    return cachedValue;
                }
            }
            return default(T);
        }

        private bool TryGetCachedValue<T>(string key, string region, out T value)
        {
            var cachedValue = _cilCacheProvider.Get(key, region);
            if (cachedValue != null)
            {
                if (!(cachedValue is T))
                {
                    throw new DxaException(
                        $"Cached value for key '{key}' in region '{region}' is of type {cachedValue.GetType().FullName} instead of {typeof(T).FullName}."
                        );
                }
                value = (T)cachedValue;
                return true;
            }
            value = default(T);
            return false;
        }

        private static string CalcLockHash(string key, string region) => $"{region}:{key}";
    }
}
