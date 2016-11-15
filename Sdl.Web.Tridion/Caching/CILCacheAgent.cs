using System.Collections.Generic;
using DD4T.ContentModel.Contracts.Caching;
using Sdl.Web.Delivery.Caching;

namespace Sdl.Web.Tridion.Caching
{
    /// <summary>
    /// DD4T Cache Agent implementation to use CIL caching
    /// </summary>
    public class CilCacheAgent : ICacheAgent
    {
        private const string CilCacheRegionName = "DXA";

        private readonly ICacheProvider<object> _cache = CacheFactory<object>.CreateFromConfiguration();

        public object Load(string key)
        {
            return _cache.Get(key, CilCacheRegionName);
        }

        public void Remove(string key)
        {
            _cache.Remove(key, CilCacheRegionName);
        }

        public void Store(string key, string region, object item, List<string> dependOnTcmUris)
        {
            _cache.Set(key, item, CilCacheRegionName, _cache.GetCacheItemPolicy(region));
        }

        public void Store(string key, string region, object item)
        {
            _cache.Set(key, item, CilCacheRegionName, _cache.GetCacheItemPolicy(region));
        }

        public void Store(string key, object item, List<string> dependOnTcmUris)
        {
            _cache.Set(key, item, CilCacheRegionName, null);
        }

        public void Store(string key, object item)
        {
            _cache.Set(key, item, CilCacheRegionName, null);
        }
    }
}
