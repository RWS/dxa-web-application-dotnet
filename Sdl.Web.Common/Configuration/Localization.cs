using System.Text.RegularExpressions;
using Sdl.Web.Common.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;

namespace Sdl.Web.Common.Configuration
{
    /// <summary>
    /// Represents a "Localization" - a Site or variant (e.g. language).
    /// </summary>
    public class Localization
    {
        private string _path;
        private string _culture;
        private static readonly Dictionary<string, Regex> _staticContentUrlRegexes = new Dictionary<string, Regex>();

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
        
        public string Culture { 
            get
            {
                return _culture;
            }
            set
            {
                try
                {
                    _culture = value;
                    CultureInfo = new CultureInfo(value);
                }
                catch (Exception e)
                {
                    Log.Error("Failed to set the Culture of Localization {0} to '{1}': {2}", this, value, e.Message);
                    CultureInfo = new CultureInfo("en-US");
                    _culture = "en-US";
                }
            }
        }

        public CultureInfo CultureInfo 
        { 
            get; 
            private set; 
        }

        public string Language
        {
            get; 
            set;
        }

        public string LocalizationId
        {
            get; 
            set;
        }

        public string StaticContentUrlPattern
        {
            get; 
            set;
        }

        public bool IsStaging
        {
            get; 
            set;
        }

        public bool IsHtmlDesignPublished
        {
            get; 
            set;
        }

        public bool IsDefaultLocalization
        {
            get; 
            set;
        }

        public string Version
        {
            get; 
            set;
        }

        public List<string> DataFormats
        {
            get; 
            set;
        }

        public List<Localization> SiteLocalizations
        {
            get; 
            set;
        }

        /// <summary>
        /// Gets the date/time at which this <see cref="Localization"/> was last (re-)loaded.
        /// </summary>
        public DateTime LastLoaded
        {
            get;
            private set;
        }

        /// <summary>
        /// Ensures that the <see cref="Localization"/> is initialized.
        /// </summary>
        public void EnsureInitialized()
        {
            using (new Tracer())
            {
                lock (this)
                {
                    if (LastLoaded == DateTime.MinValue)
                    {
                        SiteConfiguration.LoadLocalization(this, loadDetails: false); // TODO
                        LastLoaded = DateTime.Now;
                    }
                }
            }
        }

        /// <summary>
        /// Forces a full reload of the <see cref="Localization"/>.
        /// </summary>
        public void Refresh()
        {
            using (new Tracer())
            {
                lock (this)
                {
                    SiteConfiguration.LoadLocalization(this, loadDetails: true); // TODO
                    LastLoaded = DateTime.Now;
                }
            }
        }

        public string GetBaseUrl() 
        {
            if (HttpContext.Current!=null)
            {
                Uri uri = HttpContext.Current.Request.Url;
                return uri.GetLeftPart(UriPartial.Authority) + Path;
            }
            return null;
        }

        /// <summary>
        /// Determines whether a given URL (path) refers to a static content item.
        /// </summary>
        /// <param name="urlPath">The URL path.</param>
        /// <returns><c>true</c> if the URL refers to a static content item.</returns>
        public bool IsStaticContentUrl(string urlPath)
        {
            return GetStaticContentUrlRegex().IsMatch(urlPath);
        }

        private Regex GetStaticContentUrlRegex()
        {
            lock (_staticContentUrlRegexes)
            {
                Regex result;
                if (_staticContentUrlRegexes.TryGetValue(LocalizationId, out result))
                {
                    return result;
                }
                result = new Regex(StaticContentUrlPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                _staticContentUrlRegexes.Add(LocalizationId, result);
                return result;
            }
        }

        #region Overrides

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return string.IsNullOrEmpty(Language) ?
                LocalizationId :
                string.Format("{0} ('{1}')", LocalizationId, Language);
        }

        #endregion

        #region Obsolete methods in DXA 1.1

        [Obsolete("Localizations are no longer fixed to a particular Domain, so this property is no longer used",true)]
        public string Domain { get; set; }
        [Obsolete("Localizations are no longer fixed to a particular Port, so this property is no longer used", true)]
        public string Port { get; set; }
        [Obsolete("Localizations are no longer fixed to a particular Protocol, so this property is no longer used", true)]
        public string Protocol { get; set; }

        #endregion

    }
}
