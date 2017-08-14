using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using DD4T.ContentModel.Contracts.Configuration;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.ContentModel.Contracts.Caching;
using DD4T.ContentModel.Factories;
using DD4T.ContentModel;

namespace DD4T.Utils.Caching
{
    /// <summary>
    /// Implementation of ICacheAgent which is connected to the Tridion cache. It will only work if the Tridion object cache is enabled. 
    /// Note that on each call, the last publish date is retrieved from the JVM in memory (through Juggernet). 
    /// There are no calls to the database, but the 'bridge' from .NET to Java is crossed every single time. Use with caution!
    /// </summary>
    public class TridionBackedCacheAgent : ICacheAgent, IObserver<ICacheEvent>, IDisposable
    {
        private readonly IDD4TConfiguration _configuration;
        private readonly ILogger _logger;
        private IPageFactory _pageFactory;
        private IComponentPresentationFactory _componentPresentationFactory;

        public TridionBackedCacheAgent(IDD4TConfiguration configuration, ILogger logger, IPageFactory pageFactory, IComponentPresentationFactory componentpresentationFactory)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");

            if (logger == null)
                throw new ArgumentNullException("logger");

            _logger = logger;
            _configuration = configuration;
            _pageFactory = pageFactory;
            _componentPresentationFactory = componentpresentationFactory;
        }

        #region properties
        private static ObjectCache Cache
        {
            get
            {
                return MemoryCache.Default;
            }
        }

        #endregion properties

        #region ICacheAgent

        /// <summary>
        /// Load object from the cache
        /// </summary>
        /// <param name="key">Identification of the object</param>
        /// <returns></returns>
        public object Load(string key)
        {
            CacheItem cacheItem = (CacheItem) Cache[key];
            if (cacheItem == null)
            {
                return null;
            }
           
            if (cacheItem.TcmUris != null)
            {
                foreach (TcmUri tcmUri in cacheItem.TcmUris)
                {
                    DateTime lpd = tcmUri.ItemTypeId == 64 ? _pageFactory.GetLastPublishedDateByUri(tcmUri.ToString()) : _componentPresentationFactory.GetLastPublishedDate(tcmUri.ToString());
                    if (lpd > cacheItem.LastPublishedDate)
                    {
                        // item has been republished or unpublished
                        Cache.Remove(key);
                        return null;
                    }
                }
            }
            return cacheItem.Item;
        }


        /// <summary>
        /// Store any object in the cache 
        /// </summary>
        /// <param name="key">Identification of the item</param>
        /// <param name="item">The object to store (can be a page, component, schema, etc) </param>
        public void Store(string key, object item)
        {
            Store(key, null, item, null);
        }

        /// <summary>
        /// Store any object in the cache with a dependency on other items in the cache
        /// </summary>
        /// <param name="key">Identification of the item</param>
        /// <param name="item">The object to store (can be a page, component, schema, etc) </param>
        /// <param name="dependOnTcmUris">List of items on which the current item depends</param>
        public void Store(string key, object item, List<string> dependOnTcmUris)
        {
            Store(key, null, item, dependOnTcmUris);
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
            Store(key, region, item, null);
        }

        /// <summary>
        /// Store an object belonging to a specific region in the cache with a dependency on other items in the cache.
        /// </summary>
        /// <param name="key">Identification of the item</param>
        /// <param name="region">Identification of the region</param>
        /// <param name="item">The object to store (can be a page, component, schema, etc) </param>
        /// <param name="dependOnTcmUris">List of items on which the current item depends</param>
        /// <remarks>The expiration time can be configured by adding an appSetting to the config with key 'CacheSettings_REGION' 
        /// (replace 'REGION' with the name of the region). If this key does not exist, the key 'CacheSettings_Default' will be used.
        /// </remarks>
        public void Store(string key, string region, object item, List<string> dependOnTcmUris)
        {
            CacheItem cacheItem;
            if (item is IPage)
            {
                cacheItem = new CacheItem (item, ((IPage)item).LastPublishedDate, dependOnTcmUris);
            }
            else if (item is IComponentPresentation)
            {
                cacheItem = new CacheItem (item, ((IComponentPresentation)item).Component.LastPublishedDate, dependOnTcmUris);
            }
            else if (item is IComponent)
            {
                cacheItem = new CacheItem(item, ((IComponent)item).LastPublishedDate, dependOnTcmUris);
            }
            else
            {
                cacheItem = new CacheItem (item);
            }
            Cache.Add(key, cacheItem, FindCacheItemPolicy(key, item, region));
        }

        #endregion

        #region private

        private string GetDependencyCacheKey(string tcmUri)
        {
            return "Dependencies:" + tcmUri;
        }
        private CacheItemPolicy FindCacheItemPolicy(string key, object item, string region)
        {
            CacheItemPolicy policy = new CacheItemPolicy();
            policy.Priority = CacheItemPriority.Default;


            int expirationSetting = 0;
            if (!string.IsNullOrEmpty(region))
            {
                //Todo: introduce regions in the IDD4TConfiguration interface
                //expirationSetting = ConfigurationHelper.GetSetting("DD4T.CacheSettings." + region, "CacheSettings_" + region);
                expirationSetting = _configuration.GetExpirationForCacheRegion(region);
            }
            policy.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(expirationSetting);
            return policy;
        }

        public void Remove(string key)
        {
            Cache.Remove(key);
        }
        #endregion

        #region IObserver
        public virtual void Subscribe(IObservable<ICacheEvent> provider)
        {
            throw new NotSupportedException("Operation is not supported by the TridionBackedCacheAgent. To subscribe to the JMSMessageProvider use the DefaultCacheAgent instead.");
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            throw error;
        }

        public void OnNext(ICacheEvent cacheEvent)
        {
            // nothing to do
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
        }
        #endregion

        internal class CacheItem
        {
            internal CacheItem(object item)
            {
                Item = item;
                TcmUris = new List<TcmUri>();
            }
            internal CacheItem(object item, DateTime lastPublishedDate, List<string> dependentTcmUris)
            {
                Item = item;
                LastPublishedDate = lastPublishedDate;
                TcmUris = new List<TcmUri>();
                foreach (var u in dependentTcmUris)
                {
                    TcmUris.Add(new TcmUri(u));
                }
            }
            internal object Item { get; private set; }
            internal DateTime LastPublishedDate { get; private set; }
            internal List<TcmUri> TcmUris { get; private set; }
        }

    }
}
