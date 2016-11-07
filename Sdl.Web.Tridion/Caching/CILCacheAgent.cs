using DD4T.ContentModel.Contracts.Caching;
using Sdl.Web.Delivery.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Tridion.Caching
{
    /// <summary>
    /// DD4T Cache Agent implementation to use CIL caching
    /// </summary>
    public class CILCacheAgent : ICacheAgent
    {
        private readonly ICacheProvider<object> _cache;

        internal CILCacheAgent()
        {
            _cache = CacheFactory<object>.CreateFromConfiguration("CIL.Caching");
        }

        public object Load(string key)
        {
            return _cache.Get(key);
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        public void Store(string key, string region, object item, List<string> dependOnTcmUris)
        {           
            _cache.Set(key, item, region);
        }

        public void Store(string key, string region, object item)
        {
            _cache.Set(key, item, region);
        }

        public void Store(string key, object item, List<string> dependOnTcmUris)
        {
            _cache.Set(key, item);
        }

        public void Store(string key, object item)
        {
            _cache.Set(key, item);
        }
    }
}
