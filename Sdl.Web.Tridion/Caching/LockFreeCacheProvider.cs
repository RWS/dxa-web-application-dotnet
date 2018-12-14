using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Utils;
using Sdl.Web.Delivery.Caching;

namespace Sdl.Web.Tridion.Caching
{
    public class LockFreeCacheProvider : ICacheProvider
    {
        private static readonly int Timeout = 15000; // lock aquire timeout in milliseconds

        [ThreadStatic]
        private static HashSet<uint> _reentries;
        [ThreadStatic]
        private static int _reentriesCount = 0;
        private readonly ICacheProvider<object> _cilCacheProvider = CacheFactory<object>.CreateFromConfiguration();
        private readonly int[] _slots = new int[257];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Store<T>(string key, string region, T value, IEnumerable<string> dependencies = null)
        {
            Debug.Assert(_cilCacheProvider != null, "_cilCacheProvider != null");
            _cilCacheProvider.Set(key, value, region);
        }

        public bool TryGet<T>(string key, string region, out T value)
        {
            uint hash = CalcSlotIndex(key, region);
            var cachedValue = GetCachedValue<T>(key, region);
            if (cachedValue != null)
            {
                value = cachedValue;
                return true;
            }

            // Spin while we wait for the lock to become available
            uint t = TimeOut.GetTime();
            while (Interlocked.CompareExchange(ref _slots[hash], 0, 0) != 0)
            {
                cachedValue = GetCachedValue<T>(key, region);
                if (cachedValue != null)
                {
                    value = cachedValue;
                    return true;
                }
                Thread.Sleep(1);
                if (TimeOut.UpdateTimeOut(t, Timeout) <= 0) break;
            }

            // Try again since another thread may of finished with this bucket
            cachedValue = GetCachedValue<T>(key, region);
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
            // Guard against a re-entrant caching call using the same cache key. 
            // In this case we know we can return the generated value directly since it will be cached 
            // at the highest level
            if (_reentries == null) _reentries = new HashSet<uint>();
            uint hashKey = CalcHash(key, region);
            if (_reentries.Contains(hashKey))
            {
                return addFunction();
            }
            try
            {
                Interlocked.Increment(ref _reentriesCount);
                _reentries.Add(hashKey);
                var cachedValue = GetCachedValue<T>(key, region);
                if (cachedValue != null) return cachedValue;
                uint hash = CalcSlotIndex(key, region);
                int threadId = Thread.CurrentThread.ManagedThreadId;
                int usedBy = Interlocked.CompareExchange(ref _slots[hash], threadId, 0);
                // If empty slot or used by current thread we can just create cache value since 
                // other threads accessing this cache key have not created it.
                if (usedBy == 0 || usedBy == threadId)
                    return CreateCacheValue<T>(hash, key, region, addFunction, dependencies);

                // Slot in use so someone else is potentially creating this cache value so spin
                // for a fixed length of time or until slot becomes free so we can grab it ourselves.
                // We also probe forwards a fixed length in slots to reduce spin time
                uint t = TimeOut.GetTime();
                while (Interlocked.CompareExchange(ref _slots[hash], threadId, 0) != 0)
                {
                    cachedValue = GetCachedValue<T>(key, region);
                    if (cachedValue != null) return cachedValue;
                    //hash += hash; // probe forward to identify a free slot
                    //hash %= (uint)_slots.Length;
                    Thread.Sleep(1);
                    if (TimeOut.UpdateTimeOut(t, Timeout) <= 0) break;
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
        private T GetCachedValue<T>(string key, string region)
        {
            var cachedValue = _cilCacheProvider.Get(key, region);
            return cachedValue != null ? (T)cachedValue : default(T);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T CreateCacheValue<T>(uint hash, string key, string region, Func<T> addFunction,
            IEnumerable<string> dependencies)
        {
            try
            {
                var cachedValue = GetCachedValue<T>(key, region);
                if (cachedValue != null) return cachedValue;
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
        private uint CalcHash(string key, string region)
            => Hash.Murmur3(CalcHashKey(key, region));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string CalcHashKey(string key, string region)
            => $"{region}:{key}";
    }
}
