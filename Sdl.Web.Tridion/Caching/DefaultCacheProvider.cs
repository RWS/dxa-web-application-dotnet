using System.Collections.Generic;
using System.Web.Mvc;
using DD4T.ContentModel.Contracts.Caching;
using DD4T.Utils.Caching;

namespace Sdl.Web.Tridion.Caching
{
    /// <summary>
    /// Default Cache Provider implementation based on CIL caching.
    /// </summary>
    public class DefaultCacheProvider : CacheProvider
    {
        private readonly CacheProvider _cacheProvider;

        public DefaultCacheProvider()
        {
            // Try to resolve the ICacheAgent implementation to see if anyone has dropped in a DD4T implementation
            // that we could use. Normally you would use our own Unity configuration for this to switch out the
            // default cache provider but to provide a seamless transition for DD4T we also support this.
            var cacheAgent = (ICacheAgent)DependencyResolver.Current.GetService(typeof(ICacheAgent));
            if (cacheAgent == null || cacheAgent is DefaultCacheAgent)
            {
                _cacheProvider = new DxaCacheProvider();
            }
            else
            {
                _cacheProvider = new DD4TCacheProvider();
            }
        }

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
            _cacheProvider.Store(key, region, value, dependencies);
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
            return _cacheProvider.TryGet(key, region, out value);
        }

        #endregion
    }
}
