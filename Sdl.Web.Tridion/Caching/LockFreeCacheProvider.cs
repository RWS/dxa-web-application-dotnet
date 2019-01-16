using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Utils;
using Sdl.Web.Delivery.Caching;

namespace Sdl.Web.Tridion.Caching
{
    public class LockFreeCacheProvider : ICacheProvider
    {
        private static readonly int WriteTimeout = 10; 
        private static readonly int ReadTimeout = 150;

        [ThreadStatic]
        private static HashSet<uint> _reentries;
        [ThreadStatic]
        private static int _reentriesCount;
        private readonly ICacheProvider<object> _cilCacheProvider = CacheFactory<object>.CreateFromConfiguration();
        private readonly int[] _slots = new int[2053];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Store<T>(string key, string region, T value, IEnumerable<string> dependencies = null)
        {
            Debug.Assert(_cilCacheProvider != null, "_cilCacheProvider != null");
            _cilCacheProvider.Set(key, value, region);
        }

        public bool TryGet<T>(string key, string region, out T value)
        {
            var hash = CalcSlotIndex(key, region);
            T cachedValue;
            if (TryGetCachedValue(key, region, out cachedValue))
            {
                value = cachedValue;
                return true;
            }

            // Spin while we wait for the lock to become available
            var t = TimeOut.GetTime();
            while (Interlocked.CompareExchange(ref _slots[hash], 0, 0) != 0)
            {
                if (TryGetCachedValue(key, region, out cachedValue))
                {
                    value = cachedValue;
                    return true;
                }
                if (TimeOut.UpdateTimeOut(t, ReadTimeout) <= 0) break;
            }

            // Try again since another thread may of finished with this bucket
            if (TryGetCachedValue(key, region, out cachedValue))
            {
                value = cachedValue;
                return true;
            }
            value = default(T);
            return false;
        }

        public T GetOrAdd<T>(string key, string region, Func<T> addFunction, IEnumerable<string> dependencies = null)
        {
            // Guard against a re-entrant caching call using the same cache key. 
            // In this case we know we can return the generated value directly since it will be cached 
            // at the highest level
            if (_reentries == null) _reentries = new HashSet<uint>();
            var hashKey = CalcHash(key, region);
            if (_reentries.Contains(hashKey))
            {
                return addFunction();
            }
            try
            {
                Interlocked.Increment(ref _reentriesCount);
                _reentries.Add(hashKey);
                T cachedValue;
                if (TryGetCachedValue(key, region, out cachedValue)) return cachedValue;
                var hash = CalcSlotIndex(key, region);
                var threadId = Thread.CurrentThread.ManagedThreadId;
                var usedBy = Interlocked.CompareExchange(ref _slots[hash], threadId, 0);
                // If empty slot or used by current thread we can just create cache value since 
                // other threads accessing this cache key have not created it.
                if (usedBy == 0 || usedBy == threadId)
                    return CreateCacheValue<T>(hash, key, region, addFunction, dependencies);

                // Slot in use so someone else is potentially creating this cache value so spin
                // for a fixed length of time or until slot becomes free so we can grab it ourselves.
                // We also probe forwards a fixed length in slots to reduce spin time
                var t = TimeOut.GetTime();
                while (Interlocked.CompareExchange(ref _slots[hash], threadId, 0) != 0)
                {
                    if (TryGetCachedValue(key, region, out cachedValue)) return cachedValue;
                    if (TimeOut.UpdateTimeOut(t, WriteTimeout) <= 0) break;
                }

                return _slots[hash] == threadId ? CreateCacheValue<T>(hash, key, region, addFunction, dependencies) : addFunction();
            }
            finally
            {
                _reentries.Remove(hashKey);
                Interlocked.Decrement(ref _reentriesCount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T CreateCacheValue<T>(uint hash, string key, string region, Func<T> addFunction,
            IEnumerable<string> dependencies)
        {
            try
            {
                T cachedValue;
                if (TryGetCachedValue<T>(key, region, out cachedValue)) return cachedValue;
                var value = addFunction();
                Store(key, region, value, dependencies);
                return value;
            }
            finally
            {
                Interlocked.CompareExchange(ref _slots[hash], 0, Thread.CurrentThread.ManagedThreadId);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint CalcSlotIndex(string key, string region)
            => ((CalcHash(key, region) % (uint)_slots.Length) * (uint)_reentriesCount) % (uint)_slots.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint CalcHash(string key, string region)
            => Hash.Murmur3(CalcHashKey(key, region));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string CalcHashKey(string key, string region)
            => $"{region}:{key}";
    }
}
