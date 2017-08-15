using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using DD4T.ContentModel.Contracts.Configuration;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.ContentModel.Contracts.Caching;
using System.IO;

namespace DD4T.Utils.Caching
{
    /// <summary>
    /// Default implementation of ICacheAgent, as used by the factories in DD4T.Factories. It uses the System.Runtime.Caching API introduced in .NET 4. This will run in a web environment as well as a windows service, console application or any other type of environment.
    /// </summary>
    public class DefaultCacheAgent : ICacheAgent, IObserver<ICacheEvent>, IDisposable
    {
        private readonly IDD4TConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly ILogger _backgroundlogger;
        private IDisposable unsubscriber;

        public DefaultCacheAgent(IDD4TConfiguration configuration, ILogger logger)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");

            if (logger == null)
                throw new ArgumentNullException("logger");

            _logger = logger;
            // add hardcoded file system logger because for some reason log4net does not work on a background thread
            _backgroundlogger = new FSLogger();
            _configuration = configuration;
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
            return Cache[key];
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
            Cache.Add(key, item, FindCacheItemPolicy(key, item, region));
            if (dependOnTcmUris != null)
            {
                foreach (string tcmUri in dependOnTcmUris)
                {
                    TcmUri u = new TcmUri(tcmUri);
                    string lookupkey = string.Format("{0}:{1}", u.PublicationId, u.ItemId);  // Tridion communicates about cache expiry using a key like 6:1120 (pubid:itemid)
                    IList<string> dependentItems = (IList<string>)Cache[GetDependencyCacheKey(lookupkey)];
                    if (dependentItems == null)
                    {
                        dependentItems = new List<string>();
                        dependentItems.Add(key);
                        Cache.Add(GetDependencyCacheKey(lookupkey), dependentItems, DateTimeOffset.MaxValue);
                        continue;
                    }
                    if (!dependentItems.Contains(key))
                    {
                        dependentItems.Add(key);
                    }
                }
            }
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
            _logger.Debug($"subscribing to provider {provider.GetType().Name}", LoggingCategory.Background);
            unsubscriber = provider.Subscribe(this);
        }

        public void OnCompleted()
        {
            _logger.Debug("Called OnCompleted");
            // todo: see what this does....
        }

        public void OnError(Exception error)
        {
            throw error;
        }


        private static object lockOnDependencyList = new object();
        public void OnNext(ICacheEvent cacheEvent)
        {
            _backgroundlogger.Debug("received event with region {0}, uri {1} and type {2}", LoggingCategory.Background, cacheEvent.RegionPath, cacheEvent.Key, cacheEvent.Type);
            // get the list of dependent items from the cache
            // NOTE: locking is not a problem here since this code is always running on a background thread (QS)
            lock (lockOnDependencyList)
            {
                IList<string> dependencies = (IList<string>)Cache[GetDependencyCacheKey(cacheEvent.Key)];
                if (dependencies != null)
                {
                    foreach (var cacheKey in dependencies)
                    {
                        Cache.Remove(cacheKey);
                        _backgroundlogger.Debug("Removed item from cache (key = {0})", LoggingCategory.Background, cacheKey);

                    }
                    Cache.Remove(GetDependencyCacheKey(cacheEvent.Key));
                }
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (unsubscriber != null)
            {
                unsubscriber.Dispose();
            }
        }
        #endregion

    }
    internal class FSLogger : ILogger
    {
        public void Critical(string message, params object[] parameters)
        {
            WriteMessage($"FATAL - {message}", parameters);
        }

        public void Critical(string message, LoggingCategory category, params object[] parameters)
        {
            WriteMessage($"FATAL - {category} - {message}", parameters);
        }

        public void Debug(string message, params object[] parameters)
        {
            WriteMessage($"DEBUG - {message}", parameters);
        }

        public void Debug(string message, LoggingCategory category, params object[] parameters)
        {
            WriteMessage($"DEBUG - {category} - {message}", parameters);
        }

        public void Error(string message, params object[] parameters)
        {
            WriteMessage($"ERROR - {message}", parameters);
        }

        public void Error(string message, LoggingCategory category, params object[] parameters)
        {
            WriteMessage($"ERROR - {category} - {message}", parameters);
        }

        public void Information(string message, params object[] parameters)
        {
            WriteMessage($"INFO - {message}", parameters);
        }

        public void Information(string message, LoggingCategory category, params object[] parameters)
        {
            WriteMessage($"INFO - {category} - {message}", parameters);
        }

        public void Warning(string message, params object[] parameters)
        {
            WriteMessage($"WARNING - {message}", parameters);
        }

        public void Warning(string message, LoggingCategory category, params object[] parameters)
        {
            WriteMessage($"WARNING - {category} - {message}", parameters);
        }

        private static string LogFilePath
        {
            get
            {
                return Path.Combine(Path.GetTempPath(), "DD4T-DEBUG.log");
            }
        }
        private void WriteMessage(string message, params object[] parameters)
        {
            var m = string.Format(message, parameters);
            if (!File.Exists(LogFilePath))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(LogFilePath))
                {
                    sw.WriteLine(m);
                }
                return;
            }

            // This text is always added, making the file longer over time
            // if it is not deleted.
            using (StreamWriter sw = File.AppendText(LogFilePath))
            {
                sw.WriteLine(m);
            }
        }
    }
}
