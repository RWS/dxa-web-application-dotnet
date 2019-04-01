using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Sdl.Web.Delivery.Caching;

namespace Sdl.Web.Tridion.Caching
{
    /// <summary>
    /// Cache Provider implementation to forward requests to CIL caching
    /// </summary>
    public class DxaCacheProvider : CacheProvider
    {
        private readonly ICacheProvider<object> _cilCacheProvider = CacheFactory<object>.CreateFromConfiguration();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Store<T>(string key, string region, T value, IEnumerable<string> dependencies = null)
        {
            Debug.Assert(_cilCacheProvider != null, "_cilCacheProvider != null");
            _cilCacheProvider.Remove(key, region);
            _cilCacheProvider.Set(key, value, region);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override object Get(string key, string region, IEnumerable<string> dependencies = null) 
            => _cilCacheProvider.Get(key, region);
    }
}
