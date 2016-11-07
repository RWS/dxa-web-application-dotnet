using System;
using System.Collections.Generic;
using DD4T.ContentModel.Contracts.Caching;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Tridion.Mapping;

namespace Sdl.Web.Tridion.Caching
{
    /// <summary>
    /// DD4T Cache Provider implementation.
    /// </summary>
    public class DD4TCacheProvider : ICacheProvider, ICacheAgentProvider
    {
        private readonly ICacheProvider _cacheProvider;
        private readonly ICacheAgent _cacheAgent = DD4TFactoryCache.CreateDefaultCacheAgent();

        public DD4TCacheProvider()
        {
            _cacheProvider = new ThreadSafeCacheProviderAdaptor(_cacheAgent);
        }

        public ICacheAgent CacheAgent
        {
            get
            {
                return _cacheAgent;
            }
        }

        public void Store<T>(string key, string region, T value, IEnumerable<string> dependencies = null)
        {
            _cacheProvider.Store<T>(key, region, value, dependencies);
        }

        public bool TryGet<T>(string key, string region, out T value)
        {
            return _cacheProvider.TryGet<T>(key, region, out value);
        }

        public T GetOrAdd<T>(string key, string region, Func<T> addFunction, IEnumerable<string> dependencies = null)
        {
            return _cacheProvider.GetOrAdd<T>(key, region, addFunction, dependencies);                
        }
    }
}
