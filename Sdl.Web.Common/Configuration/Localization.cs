using System;
using System.Collections.Generic;
using System.Web;

namespace Sdl.Web.Common.Configuration
{
    public class Localization
    {
        private string _path;
        public string LocalizationId { get; set;}
        public string Path {
            get
            {
                return _path;
            }
            set
            {
                _path = value != null && value.EndsWith("/") ? value.Substring(0, value.Length - 1) : value;
            }
        }
        public string Culture { get; set; }
        public string Language { get; set; }
        public string MediaUrlRegex { get; set; }
        public bool IsStaging { get; set; }
        public bool IsHtmlDesignPublished { get; set; }
        public bool IsDefaultLocalization { get; set; }
        public string Version { get; set; }
        public List<Localization> SiteLocalizations { get; set; }

        public string GetBaseUrl() 
        {
            if (HttpContext.Current!=null)
            {
                var uri = HttpContext.Current.Request.Url;
                return uri.GetLeftPart(UriPartial.Authority) + Path;
            }
            return null;
        }

        #region Obsolete methods

        [Obsolete("Localizations are no longer fixed to a particular Domain, so this property is no longer used",true)]
        public string Domain { get; set; }
        [Obsolete("Localizations are no longer fixed to a particular Port, so this property is no longer used", true)]
        public string Port { get; set; }
        [Obsolete("Localizations are no longer fixed to a particular Protocol, so this property is no longer used", true)]
        public string Protocol { get; set; }

        #endregion
    }
}
