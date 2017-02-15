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
using Sdl.Web.DataModel.Configuration;

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
        private IDictionary<string, IDictionary<string, string>> _config;
        private IDictionary _resources;
        private IDictionary<string, string[]> _includePageUrls;
        private XpmRegion[] _xpmRegionConfiguration;
        private IDictionary<string, XpmRegion> _xpmRegionConfigurationMap;
        private SemanticSchema[] _semanticSchemas;
        private IDictionary<string, SemanticSchema> _semanticSchemaMap;
        private SemanticVocabulary[] _semanticVocabularies;
        private IDictionary<string, SemanticVocabulary> _semanticVocabularyMap;
        private readonly object _loadLock = new object();

        #region Nested classes
        /// <summary>
        /// Represents the (JSON) data for versioning as stored in /version.json.
        /// </summary>
        private class VersionData
        {
            [JsonProperty("version")]
            public string Version { get; set; }
        }
        #endregion

        /// <summary>
        /// Gets the Localization Identifier.
        /// </summary>
        /// <remarks>
        /// This corresponds to the (numeric) CM Publication Identifier. That is: the middle number in the Publication TCM URI.
        /// </remarks>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the URL Path of the Localization.
        /// </summary>
        /// <value>
        /// Is empty for a root-level Localization. It never ends with a slash.
        /// </value>
        /// <remarks>
        /// This property should only be set by the DXA Framework itself (in particular: by Localization Resolvers).
        /// </remarks>
        public string Path
        {
            get
            {
                return _path;
            }
            set
            {
                string canonicalPath = (value != null) && value.EndsWith("/") ? value.Substring(0, value.Length - 1) : value;
                _path = canonicalPath;
            }
        }

        /// <summary>
        /// Gets the Culture/Locale of the Localization as a string value.
        /// </summary>
        /// <remarks>
        /// The value is obtained from CM: the <c>core.culture</c> configuration value.
        /// It is used by the <see cref="CultureInfo"/> property and also as Language (!) of Atom/RSS feeds.
        /// For that reason, it must be a valid language tag as defined by Microsoft: https://msdn.microsoft.com/en-us/library/cc233982.aspx
        /// </remarks>
        /// <seealso cref="CultureInfo"/>
        public string Culture
        {
            get
            {
                return _culture;
            }
            private set
            {
                _culture = value;
                try
                {
                    CultureInfo = new CultureInfo(value);
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to set the Culture of Localization {0} to '{1}': {2}", this, value, ex.Message);
                }
            }
        }

        /// <summary>
        /// Get the Culture/Locale of the Localization as a <see cref="CultureInfo"/> object.
        /// </summary>
        /// <remarks>
        /// The Culture/Locale is used to format dates (e.g. by <see cref="HtmlHelperExtensions.Date"/>) and numbers.
        /// </remarks>
        /// <seealso cref="Culture"/>
        public CultureInfo CultureInfo { get; private set; }

        /// <summary>
        /// Gets the Language of the Localization.
        /// </summary>
        /// <remarks>
        /// The value is obtained from CM: the <c>core.language</c> configuration value.
        /// Is used for display purposes and doesn't have to conform to any standard.
        /// </remarks>
        /// <seealso cref="Culture"/>
        public string Language { get; private set; }

        /// <summary>
        /// Gets the URI scheme used for CM URIs.
        /// </summary>
        /// <remarks>
        /// Is always "tcm" for now, but can also become "ish" in the future (KC Web App support).
        /// </remarks>
        public string CmUriScheme => "tcm";

        /// <summary>
        /// Gets the Localization Identifier.
        /// </summary>
        /// <seealso cref="Id"/>
        [Obsolete("Deprecated in DXA 2.0. Use Id property instead.")]
        public string LocalizationId
        {
            get { return Id; }
            set { Id = value; }
        }

        /// <summary>
        /// Gets the URL pattern (Regular Expression) used to determine if a URL represents a Static Content Item.
        /// </summary>
        public string StaticContentUrlPattern { get; private set; }


        /// <summary>
        /// Gets the root folder of the binaries cache for this Localization.
        /// </summary>
        public string BinaryCacheFolder
            => $"{SiteConfiguration.StaticsFolder}\\{Id}";

        /// <summary>
        /// Gets (or sets) whether the Localization is XPM Enabled (a.k.a. a "Staging" environment).
        /// </summary>
        public bool IsXpmEnabled { get; set; }

        /// <summary>
        /// Gets or sets whether the Localization represents a "Staging" environment.
        /// </summary>
        // TODO TSI-878: [Obsolete("Deprecated in DXA 2.0. Use IsXpmEnabled property instead.")]
        public bool IsStaging
        {
            get { return IsXpmEnabled; }
            set { IsXpmEnabled = value; }
        }

        /// <summary>
        /// Gets whether the Localization has an HTML Design which is published from CM.
        /// </summary>
        public bool IsHtmlDesignPublished { get; private set; }

        /// <summary>
        /// Gets whether the Localization is the default one in the set of "Site Localizations"
        /// </summary>
        /// <seealso cref="SiteLocalizations"/>
        public bool IsDefaultLocalization { get; private set; }

        /// <summary>
        /// Gets the version of the HTML Design.
        /// </summary>
        /// <remarks>
        /// The version is obtained from a <c>version.json</c> file.
        /// </remarks>
        /// <seealso cref="IsHtmlDesignPublished"/>
        public string Version { get; private set; }

        /// <summary>
        /// Gets the Data Formats supported in this Localization.
        /// </summary>
        public List<string> DataFormats { get; private set; }

        /// <summary>
        /// Gets the "Site Localizations": a list of Localizations in the same "Site Group".
        /// </summary>
        /// <remarks>
        /// A typical use case is a multi-language site consisting of separate Localizations for each language.
        /// </remarks>
        public List<Localization> SiteLocalizations { get; private set; }

        /// <summary>
        /// Gets the date/time at which this <see cref="Localization"/> was last (re-)loaded.
        /// </summary>
        public DateTime LastRefresh { get; private set; }

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
                        try
                        {
                            localization.Refresh();
                        }
                        catch (Exception ex)
                        {
                            // Localization may not be published (yet). Log the exception and continue.
                            Log.Error(ex);
                        }
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
                    throw new DxaException($"Configuration key '{key}' is in the wrong format. It must be in the format [section].[key]");
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
        /// Gets an absolute (server-relative) URL path for a given context-relative URL path.
        /// </summary>
        /// <param name="contextRelativeUrlPath">The context-relative URL path. Should not start with a slash.</param>
        /// <returns>The absolute URL path.</returns>
        public string GetAbsoluteUrlPath(string contextRelativeUrlPath)
            => (contextRelativeUrlPath.StartsWith("/")) ? Path + contextRelativeUrlPath : $"{Path}/{contextRelativeUrlPath}";

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
            return $"{Path}/{SiteConfiguration.SystemFolder}/{Version}/{relativePath}";
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
                        $"Localization [{this}] does not contain includes for Page Type '{pageTypeIdentifier}'. {Constants.CheckSettingsUpToDate}"
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
                    semanticSchema.Initialize(this);
                }
            }

            SemanticSchema result;
            if (!_semanticSchemaMap.TryGetValue(schemaId, out result))
            {
                throw new DxaException(
                    $"Semantic schema '{schemaId}' not defined in Localization [{this}]. {Constants.CheckSettingsUpToDate}"
                    );
            }

            return result;
        }

        /// <summary>
        /// Gets the Semantic Vocabularies
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

        /// <summary>
        /// Gets a Semantic Vocabulary by a given prefix.
        /// </summary>
        /// <param name="prefix">The vocabulary prefix.</param>
        /// <returns>The Semantic Vocabulary.</returns>
        public SemanticVocabulary GetSemanticVocabulary(string prefix)
        {
            if (_semanticVocabularies == null)
            {
                _semanticVocabularyMap = GetSemanticVocabularies().ToDictionary(sv => sv.Prefix);
            }

            SemanticVocabulary result;
            if (!_semanticVocabularyMap.TryGetValue(prefix, out result))
            {
                throw new DxaException($"No vocabulary defined for prefix '{prefix}' in Localization [{this}]. {Constants.CheckSettingsUpToDate}");
            }

            return result;
        }

        /// <summary>
        /// Gets a CM identifier (URI) for this Localization
        /// </summary>
        /// <returns>the CM URI.</returns>
        public string GetCmUri()
            => $"{CmUriScheme}:0-{Id}-1";

        /// <summary>
        /// Gets a CM identifier (URI) for a given Model identifier.
        /// </summary>
        /// <param name="modelId">The Model identifier.</param>
        /// <param name="itemType">The item type identifier used in the CM URI.</param>
        /// <returns>The CM URI.</returns>
        public string GetCmUri(string modelId, int itemType = 16)
            => (itemType == 16) ? $"{CmUriScheme}:{Id}-{modelId}" : $"{CmUriScheme}:{Id}-{modelId}-{itemType}";

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

                    string urlPath = (relativeUrl.StartsWith("/")) ? Path + relativeUrl : $"{Path}/{SiteConfiguration.SystemFolder}/{relativeUrl}";
                    string jsonData = SiteConfiguration.ContentProvider.GetStaticContentItem(urlPath, this).GetText();
                    deserializedObject = JsonConvert.DeserializeObject<T>(jsonData);
                }
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

                    IsDefaultLocalization = localizationData.IsDefaultLocalization;
                    IsXpmEnabled = localizationData.IsXpmEnabled;

                    if (localizationData.MediaRoot != null)
                    {
                        string mediaRoot = localizationData.MediaRoot;
                        if (!mediaRoot.StartsWith("/"))
                        {
                            // SDL Web 8 context-relative URL
                            mediaRoot = $"{Path}/{mediaRoot}";
                        }
                        if (!mediaRoot.EndsWith("/"))
                        {
                            mediaRoot += "/";
                        }
                        mediaPatterns.Add($"^{mediaRoot}.*");
                    }

                    if (localizationData.SiteLocalizations != null)
                    {
                        ILocalizationResolver localizationResolver = SiteConfiguration.LocalizationResolver;
                        SiteLocalizations = new List<Localization>();
                        foreach (SiteLocalizationData siteLocalizationData in localizationData.SiteLocalizations)
                        {
                            try
                            {
                                Localization siteLocalization = localizationResolver.GetLocalization(siteLocalizationData.Id);
                                if (siteLocalization.LastRefresh == DateTime.MinValue)
                                {
                                    // Localization is not fully initialized yet; partially initialize it using the Site Localization Data.
                                    siteLocalization.Id = siteLocalizationData.Id;
                                    siteLocalization.Path = siteLocalizationData.Path;
                                    siteLocalization.IsDefaultLocalization = siteLocalizationData.IsMaster;
                                    siteLocalization.Language = siteLocalizationData.Language;
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
                        mediaPatterns.Add($"^{Path}/{SiteConfiguration.SystemFolder}/assets/.*");
                    }
                    mediaPatterns.Add($"^{Path}/{SiteConfiguration.SystemFolder}/.*\\.json$");

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

                    string configItemUrl = $"{Path}/{SiteConfiguration.SystemFolder}/config/{sectionName}.json";
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
                BootstrapData resourcesData = null;
                LoadStaticContentItem("resources/_all.json", ref resourcesData);

                _resources = new Hashtable();
                foreach (string staticContentItemUrl in resourcesData.Files)
                {
                    string type = staticContentItemUrl.Substring(staticContentItemUrl.LastIndexOf("/", StringComparison.Ordinal) + 1);
                    type = type.Substring(0, type.LastIndexOf(".", StringComparison.Ordinal)).ToLower();
                    string resourcesJson = SiteConfiguration.ContentProvider.GetStaticContentItem(staticContentItemUrl, this).GetText();
                    IDictionary<string, object> resources = JsonConvert.DeserializeObject<Dictionary<string, object>>(resourcesJson);
                    foreach (KeyValuePair<string, object> resource in resources)
                    {
                        //we ensure resource key uniqueness by adding the type (which comes from the filename)
                        _resources.Add($"{type}.{resource.Key}", resource.Value);
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
            => string.IsNullOrEmpty(Language) ? Id : $"{Id} ('{Language}')";

        #endregion
    }
}
