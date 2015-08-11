using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Helpers;
using System.Web.Script.Serialization;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;

namespace Sdl.Web.Common.Configuration
{
    /// <summary>
    /// Represents a "Localization" - a Site or variant (e.g. language).
    /// </summary>
    public class Localization
    {
        private string _path;
        private string _culture;
        private Regex _staticContentUrlRegex;
        private Dictionary<string, Dictionary<string, string>> _config;
        private readonly object _loadLock = new object();

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
        public DateTime LastRefresh
        {
            get;
            private set;
        }

        /// <summary>
        /// Ensures that the <see cref="Localization"/> is initialized.
        /// </summary>
        public void EnsureInitialized()
        {
            // No Tracer here; Load already has a Tracer.
            // Note that Load itself also obtains a load lock, but we might get a race condition on LastRefresh if we don't lock here too.
            lock (_loadLock)
            {
                if (LastRefresh == DateTime.MinValue)
                {
                    Load();
                }
            }
        }

        /// <summary>
        /// Forces a refresh/reload of the <see cref="Localization"/> and its associated configuration.
        /// </summary>
        public void Refresh(bool allSiteLocalizations = false)
        {
            using (new Tracer(allSiteLocalizations, this))
            {
                if (allSiteLocalizations)
                {
                    // Reload all Site Localizations (variants)
                    foreach (Localization localization in SiteLocalizations)
                    {
                        localization.Load();
                    }
                }
                else
                {
                    // Reload only this Localization
                    Load();
                }
            }
        }

        /// <summary>
        /// Gets a configuration value with a given key.
        /// </summary>
        /// <param name="key">The configuration key, in the format section.name.</param>
        /// <returns>The configuration value.</returns>
        public string GetConfigValue(string key)
        {
            using (new Tracer(key, this))
            {
                string[] keyParts = key.Split('.');
                if (keyParts.Length < 2)
                {
                    throw new DxaException(
                        string.Format("Configuration key '{0}' is in the wrong format. It must be in the format [section].[key]", key)
                        );
                }

                //We actually allow more than one . in the key (for example core.schemas.article) in this case the section
                //is the part up to the last dot and the key is the part after it.
                string sectionName = key.Substring(0, key.LastIndexOf(".", StringComparison.Ordinal));
                string propertyName = keyParts[keyParts.Length - 1];

                Dictionary<string, string> sectionConfig;
                if (!_config.TryGetValue(sectionName, out sectionConfig))
                {
                    throw new DxaException(string.Format("Configuration section '{0}' does not exist for Localization [{1}].", sectionName, this));
                }
                string configValue;
                if (!sectionConfig.TryGetValue(propertyName, out configValue))
                {
                    Log.Warn("Configuration key '{0}' does not exist in section '{1}' for Localization [{2}]. GetConfigValue returns null.", propertyName, sectionName, this);
                }
                return configValue;
            }
        }

        private void Load() 
        {
            using (new Tracer(this))
            {
                lock (_loadLock)
                {
                    // NOTE: intentionally setting LastRefresh first, because while loading other classes (e.g. BinaryFileManager) may be using LastRefresh to detect stale content.
                    LastRefresh = DateTime.Now;

                    IsHtmlDesignPublished = true;
                    List<string> mediaPatterns = new List<string>();
                    string versionUrl = System.IO.Path.Combine(Path.ToCombinePath(true), @"version.json").Replace("\\", "/");
                    string versionJson = SiteConfiguration.ContentProvider.GetStaticContentItem(versionUrl, this).GetText();
                    if (versionJson == null)
                    {
                        //it may be that the version json file is 'unmanaged', ie just placed on the filesystem manually
                        //in which case we try to load it directly - the HTML Design is thus not published from CMS
                        string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SiteConfiguration.SystemFolder, @"assets\version.json");
                        if (File.Exists(path))
                        {
                            versionJson = File.ReadAllText(path);
                            IsHtmlDesignPublished = false;
                        }
                    }
                    if (versionJson != null)
                    {
                        Version = Json.Decode(versionJson).version;
                    }
                    dynamic bootstrapJson = GetConfigBootstrapJson();
                    if (bootstrapJson != null)
                    {
                        //The _all.json file contains a reference to all other configuration files
                        if (bootstrapJson.defaultLocalization != null && bootstrapJson.defaultLocalization)
                        {
                            IsDefaultLocalization = true;
                        }
                        if (bootstrapJson.staging != null && bootstrapJson.staging)
                        {
                            IsStaging = true;
                        }
                        if (bootstrapJson.mediaRoot != null)
                        {
                            string mediaRoot = bootstrapJson.mediaRoot;
                            if (!mediaRoot.EndsWith("/"))
                            {
                                mediaRoot += "/";
                            }
                            mediaPatterns.Add(String.Format("^{0}{1}.*", mediaRoot, mediaRoot.EndsWith("/") ? String.Empty : "/"));
                        }
                        if (bootstrapJson.siteLocalizations != null)
                        {
                            ILocalizationResolver localizationResolver = SiteConfiguration.LocalizationResolver;
                            SiteLocalizations = new List<Localization>();
                            foreach (dynamic item in bootstrapJson.siteLocalizations)
                            {
                                string localizationId = item.id ?? item;
                                try
                                {
                                    Localization siteLocalization = localizationResolver.GetLocalization(localizationId);
                                    siteLocalization.IsDefaultLocalization = item.isMaster ?? false;
                                    SiteLocalizations.Add(siteLocalization);
                                }
                                catch (DxaUnknownLocalizationException)
                                {
                                    Log.Error("Unknown localization ID '{0}' specified in SiteLocalizations for Localization [{1}].", localizationId, this);
                                }
                            }
                        }
                        if (IsHtmlDesignPublished)
                        {
                            mediaPatterns.Add("^/favicon.ico");
                            mediaPatterns.Add(String.Format("^{0}/{1}/assets/.*", Path, SiteConfiguration.SystemFolder));
                        }
                        if (bootstrapJson.files != null)
                        {
                            List<string> configFiles = new List<string>();
                            foreach (string file in bootstrapJson.files)
                            {
                                configFiles.Add(file);
                            }
                            LoadConfig(configFiles);
                        }
                        mediaPatterns.Add(String.Format("^{0}/{1}/.*\\.json$", Path, SiteConfiguration.SystemFolder));
                    }
                    StaticContentUrlPattern = String.Join("|", mediaPatterns);
                    _staticContentUrlRegex = new Regex(StaticContentUrlPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

                    Culture = GetConfigValue("core.culture");
                    Language = GetConfigValue("core.language");
                    string formats = GetConfigValue("core.dataFormats");
                    DataFormats = formats == null ? new List<string>() : formats.Split(',').Select(f => f.Trim()).ToList();
                }
            }
        }


        private dynamic GetConfigBootstrapJson()
        {
            string url = System.IO.Path.Combine(Path.ToCombinePath(true), SiteConfiguration.SystemFolder, @"config\_all.json").Replace("\\", "/");
            string jsonData = SiteConfiguration.ContentProvider.GetStaticContentItem(url, this).GetText();
            return Json.Decode(jsonData);
        }


        private void LoadConfig(IEnumerable<string> configItemUrls)
        {
            _config = new Dictionary<string, Dictionary<string, string>>();
            foreach (string configItemUrl in configItemUrls)
            {
                string sectionName = configItemUrl.Substring(configItemUrl.LastIndexOf("/", StringComparison.Ordinal) + 1);
                sectionName = sectionName.Substring(0, sectionName.LastIndexOf(".", StringComparison.Ordinal)).ToLower();
                string jsonData = SiteConfiguration.ContentProvider.GetStaticContentItem(configItemUrl, this).GetText();
                Dictionary<string, string> settings = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(jsonData);
                _config.Add(sectionName, settings);
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
            return _staticContentUrlRegex.IsMatch(urlPath);
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
