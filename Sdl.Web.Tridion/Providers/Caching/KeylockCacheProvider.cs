using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Delivery.Caching;

namespace Sdl.Web.Tridion.Caching
{
    public class KeylockCacheProvider : ICacheProvider
    {
        private static readonly ConcurrentDictionary<string, object> KeyLocks = new ConcurrentDictionary<string, object>();

        private readonly ICacheProvider<object> _cilCacheProvider = CacheFactory<object>.CreateFromConfiguration();

        public void Store<T>(string key, string region, T value, IEnumerable<string> dependencies = null)
        {
            Debug.Assert(_cilCacheProvider != null, "_cilCacheProvider != null");

            // prevent deadlocks if we're storing something here and in GetOrAdd.
            var hash = CalcHashKey(key, region);
            lock (KeyLocks.GetOrAdd(hash, new object()))
            {
                try
                {
                    _cilCacheProvider.Set(key, value, region);
                }
                finally
                {
                    // We don't need the lock anymore
                    object tempKeyLock;
                    KeyLocks.TryRemove(hash, out tempKeyLock);
                }
            }
        }

        public bool TryGet<T>(string key, string region, out T value) => TryGetCachedValue(key, region, out value);

        public T GetOrAdd<T>(string key, string region, Func<T> addFunction, IEnumerable<string> dependencies = null)
        {
            T cachedValue;
            if (TryGetCachedValue(key, region, out cachedValue)) return cachedValue;
            var hash = CalcHashKey(key, region);
            lock (KeyLocks.GetOrAdd(hash, new object()))
            {
                try
                {
                    // Try and get from cache again in case it has been added in the meantime
                    if (TryGetCachedValue(key, region, out cachedValue)) return cachedValue;

                    // Still null, so lets run Func()
                    cachedValue = addFunction();
                    if (cachedValue != null)
                    {
                        // Note that dependencies are not used?
                        Debug.Assert(_cilCacheProvider != null, "_cilCacheProvider != null");
                        _cilCacheProvider.Set(key, cachedValue, region);
                        Store(key, region, cachedValue, dependencies);
                        return cachedValue;
                    }
                }
                finally
                {
                    // We don't need the lock anymore
                    object tempKeyLock;
                    KeyLocks.TryRemove(hash, out tempKeyLock);
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

        private static string CalcHashKey(string key, string region) => $"{region}:{key}";
    }
}
