using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Sdl.Web.Mvc.Configuration
{
    /// <summary>
    /// Resolve URLs to localizations
    /// </summary>
    public class BaseLocalizationManager : ILocalizationManager
    {
        private Dictionary<string, Localization> _uniqueLocalizations = null;
        private Dictionary<string, string> _urlToLocalizationIdMap = null;
        private Dictionary<string, DateTime> _refreshData = new Dictionary<string,DateTime>();
        private readonly object _localizationLock = new object();
        
        public virtual Localization GetContextLocalization()
        {
            return WebRequestContext.Localization;
        }

        public virtual Localization GetLocalizationFromUri(Uri uri)
        {
            if (_uniqueLocalizations == null)
            {
                throw new Exception("No Localizations Loaded");
            }
            string url = uri.ToString() + "/";
            foreach (var rootUrl in _urlToLocalizationIdMap.Keys)
            {
                if (url.ToLower().StartsWith(rootUrl))
                {
                    var locId = _urlToLocalizationIdMap[rootUrl];
                    var loc = _uniqueLocalizations[locId];
                    if (loc.Culture == null)
                    {
                        UpdateLocalization(loc.LocalizationId);
                    }
                    Log.Debug("Request for {0} is from localization {1}", uri, locId);
                    return _uniqueLocalizations[locId];
                }
            }
            return null;
        }

        public virtual Localization GetLocalizationFromId(string localizationId)
        {
            if (_uniqueLocalizations == null)
            {
                throw new Exception ("No Localizations Loaded");
            }
            return _uniqueLocalizations.Values.Where(l => l.LocalizationId == localizationId).FirstOrDefault();
        }


        /// <summary>
        /// Set the localizations from a List loaded from configuration
        /// </summary>
        /// <param name="localizations">List of configuration data</param>
        public virtual void SetLocalizations(List<Dictionary<string, string>> localizations)
        {
            lock (_localizationLock)
            {
                _uniqueLocalizations = new Dictionary<string, Localization>();
                _urlToLocalizationIdMap = new Dictionary<string, string>();
                foreach (var loc in localizations)
                {
                    var locId = loc["LocalizationId"];
                    var localization = new Localization
                    {
                        Path = (!loc.ContainsKey("Path") || loc["Path"] == "/") ? String.Empty : loc["Path"].ToLower(),
                        LocalizationId = locId
                    };
                    var protocol = !loc.ContainsKey("Protocol") ? "http" : loc["Protocol"].ToLower();
                    var domain = !loc.ContainsKey("Domain") ? "no-domain-in-cd_link_conf" : loc["Domain"].ToLower();
                    var port = !loc.ContainsKey("Port") ? String.Empty : loc["Port"];
                    var baseUrl = GetBaseUrl(protocol, domain, port, localization.Path);
                    if (!baseUrl.EndsWith("/"))
                    {
                        baseUrl += "/";
                    }
                    _urlToLocalizationIdMap.Add(baseUrl, locId);
                    if (!_uniqueLocalizations.ContainsKey(locId))
                    {
                        _uniqueLocalizations.Add(locId, localization);
                    }
                }
            }
        }

        private string GetBaseUrl(string protocol, string domain, string port, string path)
        {
            return String.Format("{0}://{1}{2}{3}", protocol, domain, String.IsNullOrEmpty(port) || port == "80" ? "" : ":" + port, String.IsNullOrEmpty(path) || path.StartsWith("/") ? path : "/" + path);
        }
        
        public virtual void UpdateLocalization(string localizationId, bool loadDetails = false)
        {
            var loc = GetLocalizationFromId(localizationId);
            if (loc != null)
            {
                UpdateLocalization(loc, loadDetails);
            }
        }

        public virtual void UpdateLocalization(Localization loc, bool loadDetails = false)
        {
            loc = SiteConfiguration.LoadLocalization(loc, loadDetails);
            var key = loc.LocalizationId;
            lock (_localizationLock)
            {
                if (_uniqueLocalizations.ContainsKey(key))
                {
                    _uniqueLocalizations[key] = loc;
                }
                else
                {
                    _uniqueLocalizations.Add(key, loc);
                }
                if (!_refreshData.ContainsKey(key))
                {
                    _refreshData.Add(key, DateTime.Now);
                }
                else
                {
                    _refreshData[key] = DateTime.Now;
                }
            }
        }

        public virtual DateTime GetLastLocalizationRefresh(string localizationId)
        {
            return _refreshData.ContainsKey(localizationId) ? _refreshData[localizationId] : DateTime.Now;
        }
    }
}
