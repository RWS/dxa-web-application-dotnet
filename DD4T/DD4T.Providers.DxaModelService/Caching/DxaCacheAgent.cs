using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using DD4T.ContentModel.Contracts.Caching;
using DD4T.ContentModel.Contracts.Configuration;
using DD4T.ContentModel.Contracts.Logging;
using Sdl.Web.Delivery.Caching;

namespace DD4T.Providers.DxaModelService.Caching
{
    /// <summary>
    /// DxaCacheAgent
    /// Implementation to expose the CIL caching system to DD4T. Refer to CIL library documentation on how to configure CIL caching.
    /// </summary>
    public class DxaCacheAgent : ICacheAgent
    {
        public const string Page = "Page";
        public const string ComponentPresentation = "ComponentPresentation";
        public const string Other = "Other";
        private const int WaitForAddingTimeout = 15000; // ms
        private static readonly Regex CacheAgentKeyRegex = new Regex(@"(?<region>\w+)_.+", RegexOptions.Compiled);
        private static readonly IDictionary<string, EventWaitHandle> AddingEvents = new ConcurrentDictionary<string, EventWaitHandle>();
        private readonly ICacheProvider<object> _cilCacheProvider;
        private readonly ILogger _logger;

        public DxaCacheAgent(ILogger logger, IDD4TConfiguration configuration)
        {
            _logger = logger;
            _logger?.Debug("DxaCacheAgent implementation initialised successfully.");
            // create our cache provider from CIL to make use of the new caching system provided
            // that includes distributed caching, multi-level caching, etc.
            _cilCacheProvider = CacheFactory<object>.CreateFromConfiguration();
        }

        public object Load(string key)
        {
            _logger?.Debug($"DxaCacheAgent::Load({key})");
            object result;
            string cacheRegion = DetermineCacheRegion(key);
            TryGet(key, cacheRegion, out result);
            return result;
        }

        public void Store(string key, object item) 
            => Store(key, item, null);

        public void Store(string key, object item, List<string> dependOnTcmUris)
            => Store(key, DetermineCacheRegion(key), item, dependOnTcmUris);

        public void Store(string key, string region, object item) 
            => Store(key, region, item, null);

        public void Store(string key, string region, object item, List<string> dependOnTcmUris)
        {
            if (item == null) return;
            Store<object>(key, region, item);
        }

        public void Remove(string key)
            => Store<object>(key, DetermineCacheRegion(key), null);

        /// <summary>
        /// Stores a given key/value pair to a given cache Region.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The name of the cache region. Different cache regions can have different retention policies.</param>
        /// <param name="value">The value. If <c>null</c>, this effectively removes the key from the cache.</param>
        /// <param name="dependencies">An optional set of dependent item IDs. Can be used to invalidate the cached item.</param>
        /// <typeparam name="T">The type of the value to add.</typeparam>
        public void Store<T>(string key, string region, T value, IEnumerable<string> dependencies = null)
        {
            _logger?.Debug($"DxaCacheAgent::Store({key},{region},{value})");
            _cilCacheProvider.Remove(key, region);  //TODO: remove when using latest UDP version since this is no longer needed
            if (value != null)
            {
                _cilCacheProvider.Set(key, value, region);
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
        public bool TryGet<T>(string key, string region, out T value)
        {
            object cachedValue = _cilCacheProvider.Get(key, region);
            if (cachedValue == null)
            {
                // Maybe another thread is just adding a value for the key/region...
                bool valueAdded = AwaitAddingValue(key, region);
                if (valueAdded)
                {
                    // Another thread has just added a value for the key/region. Retry.
                    cachedValue = _cilCacheProvider.Get(key, region);
                }

                if (cachedValue == null)
                {
                    _logger?.Debug("No value cached for key '{0}' in region '{1}'.", key, region);
                    value = default(T);
                    return false;
                }
            }

            if (!(cachedValue is T))
            {
                throw new DxaCacheAgentException(
                    $"Cached value for key '{key}' in region '{region}' is of type {cachedValue.GetType().FullName} instead of {typeof(T).FullName}."
                    );
            }

            value = (T)cachedValue;
            return true;
        }

        protected static string GetQualifiedKey(string key, string region) => $"{region}::{key}";

        protected bool AwaitAddingValue(string key, string region)
        {
            string qualifiedKey = GetQualifiedKey(key, region);

            // Check if another thread is adding a value for the key/region:
            EventWaitHandle addingEvent;
            if (!AddingEvents.TryGetValue(qualifiedKey, out addingEvent))
            {
                return false;
            }

            _logger?.Debug("Awaiting adding of value for key '{0}' in region '{1}' ...", key, region);
            if (!addingEvent.WaitOne(WaitForAddingTimeout))
            {
                // To facilitate diagnosis of deadlock conditions, first log a warning and then wait another timeout period.
                _logger?.Warning("Waiting for adding of value for key '{0}' in cache region '{1}' for {2} seconds.", key,
                    region, WaitForAddingTimeout/1000);
                if (!addingEvent.WaitOne(WaitForAddingTimeout))
                {
                    throw new DxaCacheAgentException(
                        $"Timeout waiting for adding of value for key '{key}' in cache region '{region}'.");
                }
            }
            _logger?.Debug("Done awaiting.");
            return true;
        }

        private static string DetermineCacheRegion(string key)
        {
            Match match = CacheAgentKeyRegex.Match(key);
            if (!match.Success) return Other;
            string region = match.Groups["region"].Value;
            return region.StartsWith("Page") ? Page : region;
        }
    }
}
