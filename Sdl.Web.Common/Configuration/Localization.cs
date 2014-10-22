using System;
using System.Collections.Generic;

namespace Sdl.Web.Common.Configuration
{
    public class Localization
    {
        public string LocalizationId { get; set;}
        public string Domain { get; set; }
        public string Port { get; set; }
        public string Path { get; set; }
        public string Protocol { get; set; }
        public string Culture { get; set; }
        public string MediaUrlRegex { get; set; }
        public bool IsStaging { get; set; }
        public bool IsHtmlDesignPublished { get; set; }
        public bool IsDefaultLocalization { get; set; }
        public string Version { get; set; }
        public DateTime LastSettingsRefresh { get; set; }
        public List<string> SiteLocalizationIds {
            get
            {
                return _siteLocalizationIds;
            }
            set
            {
                _siteLocalizationIds = value;
                _siteLocalizations = null;
            }
        }
        
        public string GetBaseUrl() 
        {
            return String.Format("{0}://{1}{2}{3}", Protocol, Domain, String.IsNullOrEmpty(Port) || Port=="80" ? "" : ":" + Port, String.IsNullOrEmpty(Path) || Path.StartsWith("/") ? Path : "/" + Path);
        }

        private List<string> _siteLocalizationIds;
        private List<Localization> _siteLocalizations;
        
        public List<Localization> GetSiteLocalizations()
        {
            if (_siteLocalizations==null)
            {
                _siteLocalizations = new List<Localization>();
                foreach (var id in _siteLocalizationIds)
                {
                    var loc = SiteConfiguration.LocalizationResolver.GetLocalizationFromId(id);
                    _siteLocalizations.Add(loc);
                }
            }
            return _siteLocalizations;
        }
    }
}
