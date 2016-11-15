using System.Collections.Generic;
using DD4T.ContentModel.Contracts.Caching;
using Sdl.Web.Delivery.Caching;

namespace Sdl.Web.Tridion.Caching
{
    /// <summary>
    /// DD4T Cache Agent implementation to use CIL caching
    /// </summary>
    public class CILCacheAgent : ICacheAgent
    {
        private readonly static string DXA_REGION = "DXA";
        private readonly ICacheProvider<object> _cache;

        internal CILCacheAgent()
        {
            _cache = CacheFactory<object>.CreateFromConfiguration("CIL.Caching");
        }

        public object Load(string key)
        {
            return _cache.Get(key, DXA_REGION);
        }

        public void Remove(string key)
        {
            _cache.Remove(key, DXA_REGION);
        }

        public void Store(string key, string region, object item, List<string> dependOnTcmUris)
        {
            _cache.Set(key, item, DXA_REGION, _cache.GetCacheItemPolicy(region));
        }

        public void Store(string key, string region, object item)
        {
            _cache.Set(key, item, DXA_REGION, _cache.GetCacheItemPolicy(region));
        }

        public void Store(string key, object item, List<string> dependOnTcmUris)
        {
            _cache.Set(key, item, DXA_REGION, null);
        }

        public void Store(string key, object item)
        {
            _cache.Set(key, item, DXA_REGION, null);
        }
    }
}
