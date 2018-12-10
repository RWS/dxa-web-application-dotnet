using System.Collections.Generic;
using System.Linq;
using DD4T.ContentModel.Contracts.Caching;
using System.Web.Mvc;

namespace Sdl.Web.Tridion.Caching
{
    /// <summary>
    /// Cache Provider implementation to forward requests to DD4T ICacheAgents
    /// </summary>
    public class DD4TCacheProvider : CacheProvider
    {
        private readonly ICacheAgent _cacheAgent;

        public DD4TCacheProvider()
        {
            _cacheAgent = (ICacheAgent)DependencyResolver.Current.GetService(typeof(ICacheAgent));
        }

        public override void Store<T>(string key, string region, T value, IEnumerable<string> dependencies = null)
        {
            List<string> dependsOnTcmUris = dependencies?.ToList();
            string cacheAgentKey = GetQualifiedKey(key, region);
            lock (_cacheAgent)
            {
                _cacheAgent.Remove(cacheAgentKey);
                if (value != null)
                {
                    _cacheAgent.Store(cacheAgentKey, region, value, dependsOnTcmUris);
                }
            }
        }

        public override object Get(string key, string region, IEnumerable<string> dependencies = null)
        {
            string cacheAgentKey = GetQualifiedKey(key, region);
            return _cacheAgent.Load(cacheAgentKey);
        }

        protected static string GetQualifiedKey(string key, string region) => $"{region}::{key}";
    }
}
