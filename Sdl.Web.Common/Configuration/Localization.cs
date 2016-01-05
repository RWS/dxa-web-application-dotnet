using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Mapping;
using Sdl.Web.Common.Models.Data;

namespace Sdl.Web.Common.Configuration
{
    /// <summary>
    /// Represents a "Localization" - a Site or variant (e.g. language).
    /// </summary>
    public class Localization
    {
        private LocalizationData _data = new LocalizationData();
        private string _culture;
        private Regex _staticContentUrlRegex;
        private IDictionary<string, IDictionary<string, string>> _config;
        private IDictionary _resources;
        private IDictionary<string, string[]> _includePageUrls;
        private XpmRegion[] _xpmRegionConfiguration;
        private IDictionary<string, XpmRegion> _xpmRegionConfigurationMap;
        private SemanticSchema[] _semanticSchemas;
        private IDictionary<string, SemanticSchema> _semanticSchemaMap;
        private SemanticVocabulary[] _semanticVocabularies;
        private readonly object _loadLock = new object();

        public string Path {
            get { return _data.Path; }
            set { _data.Path = value != null && value.EndsWith("/") ? value.Substring(0, value.Length - 1) : value; }
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
            get { return _data.Language; } 
            set { _data.Language = value; }
        }

        public string LocalizationId
        {
            get { return _data.Id; }
            set { _data.Id = value; }
        }

        public string StaticContentUrlPattern
        {
            get; 
            set;
        }

        public bool IsStaging
        {
            get { return _data.IsXpmEnabled; }
            set { _data.IsXpmEnabled = value; }
        }

        public bool IsHtmlDesignPublished
        {
            get; 
            set;
        }

        public bool IsDefaultLocalization
        {
            get { return _data.IsDefaultLocalization; }
            set { _data.IsDefaultLocalization = value; }
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
                    try
                    {
                        Load();
                    }
                    catch (Exception)
                    {
                        // If an exception occurs during Loading, the Localization should remain uninitialized.
                        LastRefresh = DateTime.MinValue;
                        throw;
                    }
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
                    // Refresh all Site Localizations (variants)
                    foreach (Localization localization in SiteLocalizations)
                    {
                        localization.Refresh();
                    }
                }
                else
                {
                    // Refresh only this Localization
                    _config = null;
                    _resources = null;
                    _includePageUrls = null;
                    _xpmRegionConfiguration = null;
                    _semanticSchemas = null;
                    _semanticVocabularies = null;
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

                IDictionary<string, string> configSection;
                if (_config == null || !_config.TryGetValue(sectionName, out configSection))
                {
                    configSection = LoadConfigSection(sectionName);
                }
                string configValue;
                if (!configSection.TryGetValue(propertyName, out configValue))
                {
                    Log.Debug("Configuration key '{0}' does not exist in section '{1}' for Localization [{2}]. GetConfigValue returns null.", propertyName, sectionName, this);
                }
                return configValue;
            }
        }

        /// <summary>
        /// Gets resources.
        /// </summary>
        /// <param name="sectionName">Optional name of the section for which to get resource. If not specified (or <c>null</c>), all resources are obtained.</param>
        public IDictionary GetResources(string sectionName = null)
        {
            // TODO PERF: use sectionName to JIT load resources 
            if (_resources == null)
            {
                lock (_loadLock)
                {
                    if (_resources == null)
                    {
                        LoadResources();
                    }
                }
            }
            return _resources;
        }

        /// <summary>
        /// Gets a versioned URL (including the version number of the HTML design/assets).
        /// </summary>
        /// <param name="relativePath">The (unversioned) URL path relative to the system folder</param>
        /// <returns>A versioned URL path (server-relative).</returns>
        /// <remarks>
        /// Versioned URLs are used to facilitate agressive caching of those assets; see StaticContentModule.
        /// </remarks>
        public string GetVersionedUrlPath(string relativePath)
        {
            if (relativePath.StartsWith("/"))
            {
                relativePath = relativePath.Substring(1);
            }
            return string.Format("{0}/{1}/{2}/{3}", Path, SiteConfiguration.SystemFolder, Version, relativePath);
        }

        /// <summary>
        /// Gets the include Page URLs for a given Page Type/Template.
        /// </summary>
        /// <param name="pageTypeIdentifier">The Page Type Identifier.</param>
        /// <returns>The URLs of Include Pages</returns>
        /// <remarks>
        /// The concept of Include Pages will be removed in a future version of DXA.
        /// As of DXA 1.1 Include Pages are represented as <see cref="Sdl.Web.Common.Models.PageModel.Regions"/>.
        /// Implementations should avoid using this method directly.
        /// </remarks>
        public IEnumerable<string> GetIncludePageUrls(string pageTypeIdentifier)
        {
            using (new Tracer(pageTypeIdentifier, this))
            {
                if (_includePageUrls == null)
                {
                    LoadStaticContentItem("mappings/includes.json", ref _includePageUrls);
                }

                string[] result;
                if (!_includePageUrls.TryGetValue(pageTypeIdentifier, out result))
                {
                    throw new DxaException(
                        String.Format("Localization [{0}] does not contain includes for Page Type '{1}'. {2}", this, pageTypeIdentifier, Constants.CheckSettingsUpToDate)
                        );
                }

                return result;
            }
        }

        /// <summary>
        /// Gets XPM Region configuration for a given Region name.
        /// </summary>
        /// <param name="regionName">The Region name</param>
        /// <returns>The XPM Region configuration or <c>null</c> if no configuration is found.</returns>
        public XpmRegion GetXpmRegionConfiguration(string regionName)
        {
            // This method is called a lot, so intentionally no Tracer here.
            if (_xpmRegionConfiguration == null)
            {
                LoadStaticContentItem("mappings/regions.json", ref _xpmRegionConfiguration);
                _xpmRegionConfigurationMap = _xpmRegionConfiguration.ToDictionary(xpmRegion => xpmRegion.Region);
            }

            XpmRegion result;
            if (!_xpmRegionConfigurationMap.TryGetValue(regionName, out result))
            {
                Log.Warn("XPM Region '{0}' is not defined in Localization [{1}].", regionName, this);
            }

            return result;
        }

        /// <summary>
        /// Gets Semantic Schema for a given schema identifier.
        /// </summary>
        /// <param name="schemaId">The schema identifier.</param>
        /// <returns>The Semantic Schema configuration.</returns>
        public SemanticSchema GetSemanticSchema(string schemaId)
        {
            // This method is called a lot, so intentionally no Tracer here.
            if (_semanticSchemas == null)
            {
                LoadStaticContentItem("mappings/schemas.json", ref _semanticSchemas);
                _semanticSchemaMap = _semanticSchemas.ToDictionary(ss => ss.Id.ToString(CultureInfo.InvariantCulture));
                foreach (SemanticSchema semanticSchema in _semanticSchemas)
                {
                    semanticSchema.Localization = this;
                }
            }

            SemanticSchema result;
            if (!_semanticSchemaMap.TryGetValue(schemaId, out result))
            {
                throw new DxaException(
                    string.Format("Semantic schema '{0}' not defined in Localization [{1}]. {2}",schemaId, this, Constants.CheckSettingsUpToDate)
                    );
            }

            return result;
        }

        /// <summary>
        /// Gets the Semantic Vocabularies (indexed by their prefix)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SemanticVocabulary> GetSemanticVocabularies()
        {
            // This method is called a lot, so intentionally no Tracer here.
            if (_semanticVocabularies == null)
            {
                LoadStaticContentItem("mappings/vocabularies.json", ref _semanticVocabularies);
            }
            return _semanticVocabularies;
        } 


        private void LoadStaticContentItem<T>(string relativeUrl, ref T deserializedObject)
        {
            using (new Tracer(relativeUrl, deserializedObject, this))
            {
                lock (_loadLock)
                {
                    // Because of "optimistic locking", the object may already have been loaded at this point.
                    if (deserializedObject != null)
                    {
                        return;
                    }

                    string urlPath = (relativeUrl.StartsWith("/")) ? Path + relativeUrl : string.Format("{0}/{1}/{2}", Path, SiteConfiguration.SystemFolder, relativeUrl);
                    string jsonData = SiteConfiguration.ContentProvider.GetStaticContentItem(urlPath, this).GetText();
                    deserializedObject = JsonConvert.DeserializeObject<T>(jsonData);
                }
            }
        }

        private void SetData(LocalizationData data)
        {
            LocalizationData oldData = _data;
            _data = data;
            if (data.Id == null)
            {
                _data.Id = oldData.Id;
            }
            if (data.Path == null)
            {
                _data.Path = oldData.Path;
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

                    List<string> mediaPatterns = new List<string>();

                    VersionData versionData = null;
                    try
                    {
                        LoadStaticContentItem("/version.json", ref versionData);
                        IsHtmlDesignPublished = true;
                    }
                    catch (DxaItemNotFoundException)
                    {
                        //it may be that the version json file is 'unmanaged', ie just placed on the filesystem manually
                        //in which case we try to load it directly - the HTML Design is thus not published from CMS
                        IsHtmlDesignPublished = false;
                        string versionJsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SiteConfiguration.SystemFolder, @"assets\version.json");
                        if (File.Exists(versionJsonPath))
                        {
                            string versionJson = File.ReadAllText(versionJsonPath);
                            versionData = JsonConvert.DeserializeObject<VersionData>(versionJson);
                            Log.Info("Obtained '{0}' directly from file system.", versionJsonPath);
                        }
                        else
                        {
                            Log.Warn("HTML design is not published nor does file '{0}' exist on disk. Setting version to v0.0", versionJsonPath);
                            versionData = new VersionData { Version = "v0.0" };
                        }
                    }
                    Version = versionData.Version;

                    LocalizationData localizationData = null;
                    LoadStaticContentItem("config/_all.json", ref localizationData);
                    SetData(localizationData);

                    if (_data.MediaRoot != null)
                    {
                        string mediaRoot = _data.MediaRoot;
                        if (!mediaRoot.EndsWith("/"))
                        {
                            mediaRoot += "/";
                        }
                        mediaPatterns.Add(string.Format("^{0}.*", mediaRoot));
                    }

                    if (_data.SiteLocalizations != null)
                    {
                        ILocalizationResolver localizationResolver = SiteConfiguration.LocalizationResolver;
                        SiteLocalizations = new List<Localization>();
                        foreach (LocalizationData siteLocalizationData in _data.SiteLocalizations)
                        {
                            try
                            {
                                Localization siteLocalization = localizationResolver.GetLocalization(siteLocalizationData.Id);
                                if (siteLocalization.LastRefresh == DateTime.MinValue)
                                {
                                    siteLocalization.SetData(siteLocalizationData);
                                }
                                SiteLocalizations.Add(siteLocalization);
                            }
                            catch (DxaUnknownLocalizationException)
                            {
                                Log.Error("Unknown localization ID '{0}' specified in SiteLocalizations for Localization [{1}].", siteLocalizationData.Id, this);
                            }
                        }
                    }

                    if (IsHtmlDesignPublished)
                    {
                        mediaPatterns.Add("^/favicon.ico");
                        mediaPatterns.Add(String.Format("^{0}/{1}/assets/.*", Path, SiteConfiguration.SystemFolder));
                    }
                    mediaPatterns.Add(String.Format("^{0}/{1}/.*\\.json$", Path, SiteConfiguration.SystemFolder));

                    StaticContentUrlPattern = String.Join("|", mediaPatterns);
                    _staticContentUrlRegex = new Regex(StaticContentUrlPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

                    Culture = GetConfigValue("core.culture");
                    Language = GetConfigValue("core.language");
                    string formats = GetConfigValue("core.dataFormats");
                    DataFormats = formats == null ? new List<string>() : formats.Split(',').Select(f => f.Trim()).ToList();
                }
            }
        }

        private IDictionary<string, string> LoadConfigSection(string sectionName)
        {
            using (new Tracer(sectionName, this))
            {
                lock (_loadLock)
                {
                    if (_config == null)
                    {
                        _config = new Dictionary<string, IDictionary<string, string>>();
                    }

                    string configItemUrl = string.Format("{0}/{1}/config/{2}.json", Path, SiteConfiguration.SystemFolder, sectionName);
                    string configJson = SiteConfiguration.ContentProvider.GetStaticContentItem(configItemUrl, this).GetText();
                    IDictionary<string, string> configSection = JsonConvert.DeserializeObject<Dictionary<string, string>>(configJson);
                    _config[sectionName] = configSection;
                    return configSection;
                }
            }
        }

        private void LoadResources()
        {
            using (new Tracer(this))
            {
                ResourcesData resourcesData = null;
                LoadStaticContentItem("resources/_all.json", ref resourcesData);

                _resources = new Hashtable();
                foreach (string staticContentItemUrl in resourcesData.StaticContentItemUrls)
                {
                    string type = staticContentItemUrl.Substring(staticContentItemUrl.LastIndexOf("/", StringComparison.Ordinal) + 1);
                    type = type.Substring(0, type.LastIndexOf(".", StringComparison.Ordinal)).ToLower();
                    string resourcesJson = SiteConfiguration.ContentProvider.GetStaticContentItem(staticContentItemUrl, this).GetText();
                    IDictionary<string, object> resources = JsonConvert.DeserializeObject<Dictionary<string, object>>(resourcesJson);
                    foreach (KeyValuePair<string, object> resource in resources)
                    {
                        //we ensure resource key uniqueness by adding the type (which comes from the filename)
                        _resources.Add(String.Format("{0}.{1}", type, resource.Key), resource.Value);
                    }
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
