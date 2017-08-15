using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.Utils.Caching
{
    public class CacheKeyFactory
    {
        public static string GenerateKeyFromUri(string uri, string cacheRegion)
        {
            return cacheRegion + "_" + uri;
        }
        //public static string GenerateKeyFromUris(IList<string> uris, string cacheRegion)
        //{
        //    return cacheRegion + "_" + uris.Aggregate((current, next) => current + "_" + next);
        //}
        public static string GenerateKey(string cacheRegion, params string[] keys)
        {
            return cacheRegion + "_" + string.Join("_", keys);
        }
    }
}
