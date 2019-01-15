using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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

        public bool TryGet<T>(string key, string region, out T value)
        {
            var cachedValue = GetCachedValue<T>(key, region);
            if (cachedValue != null)
            {
                value = cachedValue;
                return true;
            }

            value = default(T);
            return false;
        }

        public T GetOrAdd<T>(string key, string region, Func<T> addFunction, IEnumerable<string> dependencies = null)
        {
            var cachedValue = GetCachedValue<T>(key, region);

            if (cachedValue == null)
            {
                var hash = CalcHashKey(key, region);

                lock (KeyLocks.GetOrAdd(hash, new object()))
                {
                    try
                    {
                        // Try and get from cache again in case it has been added in the meantime
                        cachedValue = GetCachedValue<T>(key, region);

                        // Still null, so lets run Func()
                        if (cachedValue == null && (cachedValue = addFunction()) != null)
                        {
                            // Note that dependencies are not used?
                            Debug.Assert(_cilCacheProvider != null, "_cilCacheProvider != null");
                            _cilCacheProvider.Set(key, cachedValue, region);
                            Store(key, region, cachedValue, dependencies);
                        }
                    }
                    finally
                    {
                        // We don't need the lock anymore
                        object tempKeyLock;
                        KeyLocks.TryRemove(hash, out tempKeyLock);
                    }
                }
            }

            return cachedValue != null ? cachedValue : default(T);
        }

        private T GetCachedValue<T>(string key, string region)
        {
            var cachedValue = _cilCacheProvider.Get(key, region);

            return cachedValue == null ? default(T) : (T) cachedValue;
        }

        private static string CalcHashKey(string key, string region)
        {
           return $"{region}:{key}";
        }
    }
}