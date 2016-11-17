using System.Collections.Generic;
using System.Text.RegularExpressions;
using DD4T.ContentModel.Contracts.Caching;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;

namespace Sdl.Web.Tridion.Caching
{
    /// <summary>
    /// Adapter class which exposes a DXA Cache Provider as DD4T Cache Agent.
    /// </summary>
    internal class DD4TCacheAgentAdapter : ICacheAgent
    {
        private readonly ICacheProvider _cacheProvider = SiteConfiguration.CacheProvider;
        private static readonly Regex _cacheAgentKeyRegex = new Regex(@"(?<region>\w+)_.+", RegexOptions.Compiled);

        #region ICacheAgent members
        public object Load(string key)
        {
            object result;
            string cacheRegion = DetermineCacheRegion(key);
            _cacheProvider.TryGet(key, cacheRegion, out result);
            return result;
        }

        public void Store(string key, object item)
        {
            Store(key, item, null);
        }

        public void Store(string key, object item, List<string> dependOnTcmUris)
        {
            string cacheRegion = DetermineCacheRegion(key);
            _cacheProvider.Store(key, cacheRegion, item, dependOnTcmUris);
        }

        public void Store(string key, string region, object item)
        {
            Store(key, region, item, null);
        }

        public void Store(string key, string region, object item, List<string> dependOnTcmUris)
        {
            _cacheProvider.Store(key, region, item, dependOnTcmUris);
        }

        void ICacheAgent.Remove(string key)
        {
            string cacheRegion = DetermineCacheRegion(key);
            _cacheProvider.Store<object>(key, cacheRegion, null);
        }
        #endregion

        private static string DetermineCacheRegion(string key)
        {
            Match match = _cacheAgentKeyRegex.Match(key);
            if (!match.Success)
            {
                return CacheRegions.Other;
            }

            string region = match.Groups["region"].Value;
            return region.StartsWith("Page") ? CacheRegions.Page : region;
        }
    }
}
