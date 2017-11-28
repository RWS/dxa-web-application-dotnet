using System;
using System.Web.Caching;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Mvc.OutputCache
{
    /// <summary>
    /// Output cache provider that will make use of Dxa cache provider.
    /// </summary>
    public class DxaOutputCacheProvider : OutputCacheProvider
    {
        public override object Add(string key, object entry, DateTime utcExpiry)
        {
            SiteConfiguration.CacheProvider.Store(key, CacheRegions.RenderedOutput, entry);
            return entry;
        }

        public override object Get(string key)
        {
            object value;
            SiteConfiguration.CacheProvider.TryGet(key, CacheRegions.RenderedOutput, out value);
            return value;
        }

        public override void Remove(string key)
        {            
        }

        public override void Set(string key, object entry, DateTime utcExpiry)
        {
            SiteConfiguration.CacheProvider.Store(key, CacheRegions.RenderedOutput, entry);
        }
    }
}
