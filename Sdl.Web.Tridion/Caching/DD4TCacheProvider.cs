using System.Collections.Generic;
using System.Linq;
using DD4T.ContentModel.Contracts.Caching;
using Sdl.Web.Common;
using Sdl.Web.Common.Logging;
using Sdl.Web.Tridion.Mapping;

namespace Sdl.Web.Tridion.Caching
{
    /// <summary>
    /// Cache Provider implementation based on DD4T Default Cache Agent.
    /// </summary>
    public class DD4TCacheProvider : CacheProvider
    {
        private readonly ICacheAgent _cacheAgent = DD4TFactoryCache.CacheAgent();

        #region ICacheProvider members
        /// <summary>
        /// Stores a given key/value pair to a given cache Region.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The name of the cache region. Different cache regions can have different retention policies.</param>
        /// <param name="value">The value. If <c>null</c>, this effectively removes the key from the cache.</param>
        /// <param name="dependencies">An optional set of dependent item IDs. Can be used to invalidate the cached item.</param>
        /// <typeparam name="T">The type of the value to add.</typeparam>
        public override void Store<T>(string key, string region, T value, IEnumerable<string> dependencies = null)
        {
            List<string> dependsOnTcmUris = (dependencies == null) ? null : dependencies.ToList();
            string cacheAgentKey = GetQualifiedKey(key, region);
            lock (_cacheAgent)
            {
                _cacheAgent.Remove(cacheAgentKey);
                if (value != null)
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
        public override bool TryGet<T>(string key, string region, out T value)
        {
            string cacheAgentKey = GetQualifiedKey(key, region);
            object cachedValue = _cacheAgent.Load(cacheAgentKey);
            if (cachedValue == null)
            {
                // Maybe another thread is just adding a value for the key/region...
                bool valueAdded = AwaitAddingValue(key, region);
                if (valueAdded)
                {
                    // Another thread has just added a value for the key/region. Retry.
                    cachedValue = _cacheAgent.Load(cacheAgentKey);
                }

                if (cachedValue == null)
                {
                    Log.Debug("No value cached for key '{0}' in region '{1}'.", key, region);
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

        #endregion
    }
}
