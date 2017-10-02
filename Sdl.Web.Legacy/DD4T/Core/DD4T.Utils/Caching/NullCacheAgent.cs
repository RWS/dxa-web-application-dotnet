using System;
using System.Collections.Generic;
using DD4T.ContentModel.Contracts.Caching;


namespace DD4T.Utils.Caching
{
    /// <summary>
    /// Implementation of ICacheAgent that does not cache at all. Use this to turn off all caching.
    /// </summary>
    public class NullCacheAgent : ICacheAgent
    {

        #region ICacheAgent

        /// <summary>
        /// Load object from the cache
        /// </summary>
        /// <param name="key">Identification of the object</param>
        /// <returns></returns>
        public object Load(string key)
        {
            return null;
        }

        /// <summary>
        /// Store any object in the cache 
        /// </summary>
        /// <param name="key">Identification of the item</param>
        /// <param name="item">The object to store (can be a page, component, schema, etc) </param>
        public void Store(string key, object item)
        {
        }

        /// <summary>
        /// Store any object in the cache with a dependency on other items in the cache
        /// </summary>
        /// <param name="key">Identification of the item</param>
        /// <param name="item">The object to store (can be a page, component, schema, etc) </param>
        /// <param name="dependOnItems">List of items on which the current item depends</param>
        public void Store(string key, object item, List<string> dependOnItems)
        {
        }

        /// <summary>
        /// Store an object belonging to a specific region in the cache 
        /// </summary>
        /// <param name="key">Identification of the item</param>
        /// <param name="region">Identification of the region</param>
        /// <param name="item">The object to store (can be a page, component, schema, etc) </param>
        /// <remarks>The expiration time can be configured by adding an appSetting to the config with key 'CacheSettings_REGION' 
        /// (replace 'REGION' with the name of the region). If this key does not exist, the key 'CacheSettings_Default' will be used.
        /// </remarks>
        public void Store(string key, string region, object item)
        {
        }

        /// <summary>
        /// Store an object belonging to a specific region in the cache with a dependency on other items in the cache.
        /// </summary>
        /// <param name="key">Identification of the item</param>
        /// <param name="region">Identification of the region</param>
        /// <param name="item">The object to store (can be a page, component, schema, etc) </param>
        /// <param name="dependOnItems">List of items on which the current item depends</param>
        /// <remarks>The expiration time can be configured by adding an appSetting to the config with key 'CacheSettings_REGION' 
        /// (replace 'REGION' with the name of the region). If this key does not exist, the key 'CacheSettings_Default' will be used.
        /// </remarks>
        public void Store(string key, string region, object item, List<string> dependOnItems)
        {
        }

        public void Remove(string key)
        {
        }

        #endregion

    }
}
