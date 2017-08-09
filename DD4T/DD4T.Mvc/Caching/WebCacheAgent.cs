using System.Collections.Generic;
using DD4T.ContentModel.Contracts.Caching;
using System.Web.Caching;
using System.Web;
using System;

namespace DD4T.Factories.Caching
{
    /// <summary>
    /// Implementation of ICacheAgent meant to be used with .NET 3.5 and below. If you are able to run .NET 4, please use DD4T.Factories.Caching.DefaultCacheAgent instead.
    /// WebCacheAgent is dependent on the HttpContext, which means it cannot run inside a windows service or other type of app.
    /// </summary>

    public class WebCacheAgent : ICacheAgent
    {
        private Cache Cache
        {
            get
            {
                return HttpContext.Current.Cache;
            }
        }
        public object Load(string key)
        {
            return Cache[key];
        }

        public void Remove(string key)
        {
            Cache.Remove(key);
        }

        /// <summary>
        /// Store any object in the cache 
        /// </summary>
        /// <param name="key">Identification of the item</param>
        /// <param name="item">The object to store (can be a page, component, schema, etc) </param>
        public void Store(string key, object item)
        {
            Cache.Insert(key, item);
        }

        /// <summary>
        /// Store any object in the cache. NOTE: regions and dependent items are ignored in this implementation!
        /// </summary>
        /// <param name="key">Identification of the item</param>
        /// <param name="item">The object to store (can be a page, component, schema, etc) </param>
        /// <param name="dependOnItems">List of items on which the current item depends. IGNORED!</param>
        /// <remarks>Regions and dependent items are ignored in this implementation!</remarks>
        public void Store(string key, object item, List<string> dependOnItems)
        {
            Store(key, item);
        }

        /// <summary>
        /// Store an object belonging to a specific region in the cache. NOTE: regions and dependent items are ignored in this implementation!
        /// </summary>
        /// <param name="key">Identification of the item</param>
        /// <param name="region">Identification of the region (IGNORED!)</param>
        /// <param name="item">The object to store </param>
        /// <remarks>Regions and dependent items are ignored in this implementation!</remarks>
        public void Store(string key, string region, object item)
        {
            Store(key, item);
        }

        /// <summary>
        /// Store an object belonging to a specific region in the cache with a dependency on other items in the cache. NOTE: regions and dependent items are ignored in this implementation!
        /// </summary>
        /// <param name="key">Identification of the item</param>
        /// <param name="region">Identification of the region (IGNORED!)</param>
        /// <param name="item">The object to store </param>
        /// <param name="dependOnItems">List of items on which the current item depends (IGNORED!)</param>
        /// <remarks>Regions and dependent items are ignored in this implementation!</remarks>
        public void Store(string key, string region, object item, List<string> dependOnItems)
        {
            Store(key, item);
        }
    }
}
