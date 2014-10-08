using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Compilation;
using System.Web.Helpers;
using System.Web.Script.Serialization;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Mapping;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Configuration
{
    public enum ScreenWidth
    {
        ExtraSmall,
        Small,
        Medium,
        Large
    }

    /// <summary>
    /// General Configuration Class which reads configuration from json files on disk
    /// </summary>
    public static class SiteConfiguration
    {
        /// <summary>
        /// Media helper used for generating responsive markup for images, videos etc.
        /// </summary>
        public static IMediaHelper MediaHelper {get;set;}

        /// <summary>
        /// Static file manager used for serializing and accessing static files published from the CMS (config/resources/HTML design assets etc.)
        /// </summary>
        public static IStaticFileManager StaticFileManager { get; set; }
        
        /// <summary>
        /// A set of all the valid localizations for this site
        /// </summary>
        public static Dictionary<string, Localization> Localizations { get; set; }

        public const string VersionRegex = "(v\\d*.\\d*)";
        public const string SystemFolder = "system";
        public const string CoreModuleName = "core";
        public const string StaticsFolder = "BinaryData";
        public const string DefaultVersion = "v1.00";
        
        /// <summary>
        /// True by default, is set to false if the HTML design assets (CSS, JS etc.) are not published from the CMS
        /// </summary>
        public static bool IsHtmlDesignPublished = true;

        private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> _configuration = new Dictionary<string,Dictionary<string,Dictionary<string,string>>>();
        private static Dictionary<string, Type> _viewModelRegistry;
        private static Dictionary<string, DateTime> _localizationLastRefreshes = new Dictionary<string, DateTime>();
        
        /// <summary>
        /// A registry of View Path -> View Model Type mappings to enable the correct View Model to be mapped for a given View
        /// </summary>
        public static Dictionary<string, Type> ViewModelRegistry
        {
            get
            {
                if (_viewModelRegistry == null)
                {
                    _viewModelRegistry = new Dictionary<string, Type>();
                }
                return _viewModelRegistry;
            }
            set
            {
                _viewModelRegistry = value;
            }
        }

        /// <summary>
        /// True if this is a staging website
        /// </summary>
        [Obsolete("Use Localization.IsStaging property of current localization (eg via WebRequestContext.Localization.IsStaging)",true)]
        public static bool IsStaging { get; set; }

        /// <summary>
        /// A dictionary of local (varying per localization) configuration settings, typically accessed with the GetConfig method, or Html.Config extension method (in Views)
        /// </summary> 
        public static Dictionary<string, Dictionary<string, Dictionary<string, string>>> Configuration
        {
            get
            {
                return _configuration;
            }
        }

        private static readonly object ConfigLock = new object();
        private static readonly object ViewRegistryLock = new object();
        
        /// <summary>
        /// Gets a (global) configuration setting
        /// </summary>
        /// <param name="key">The configuration key, in the format "section.name" (eg "Schema.Article")</param>
        /// <param name="module">The module (eg "Search") - if none specified this defaults to "Core"</param>
        /// <returns>The configuration matching the key for the given module</returns>
        [Obsolete("GetGlobalConfig(string,string) is deprecated, please use GetConfig(string, Localization) by combining the key and module parameters in the format {module.key} (eg core.schemas.article)", true)]
        public static string GetGlobalConfig(string key, string module = CoreModuleName)
        {
            return GetConfig(String.Format("{0}.{1}",module,key), GetLocalizationFromPath(""));
        }

        private static Localization GetLocalizationFromPath(string path)
        {
            foreach (var loc in Localizations.Values)
            {
                if (loc.Path == path)
                {
                    return loc;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a (localized) configuration setting
        /// </summary>
        /// <param name="key">The configuration key, in the format "section.name" (eg "Environment.CmsUrl")</param>
        /// <param name="localization">The localization (eg "en", "fr") - if none specified the default is used</param>
        /// <returns>The configuration matching the key for the given localization</returns>
        [Obsolete("GetConfig(string,string) is deprecated, please use GetConfig(string, Localization) instead.", true)]
        public static string GetConfig(string key, string localization = null)
        {
            return GetConfig(key, GetLocalizationFromPath(localization ?? ""));
        }

        /// <summary>
        /// Gets a (localized) configuration setting
        /// </summary>
        /// <param name="key">The configuration key, in the format "section.name" (eg "Environment.CmsUrl")</param>
        /// <param name="localization">The localization to get config for</param>
        /// <returns>The configuration matching the key for the given localization</returns>
        public static string GetConfig(string key, Localization localization)
        {
            CheckLocalizationLoaded(localization);
            return GetConfig(Configuration, key, localization.LocalizationId);
        }

        public static void CheckLocalizationLoaded(Localization localization)
        {
            var key = localization.LocalizationId;
            if (!Configuration.ContainsKey(key) || (_localizationLastRefreshes.ContainsKey(key) && _localizationLastRefreshes[key] < localization.LastSettingsRefresh))
            {
                LoadLocalization(localization);
            }
        }
        
        private static string GetConfig(IReadOnlyDictionary<string, Dictionary<string, Dictionary<string, string>>> config, string key, string type, bool global = false)
        {
            Exception ex;
            if (config.ContainsKey(type))
            {
                var subConfig = config[type];
                var bits = key.Split('.');
                if (bits.Length >= 2)
                {
                    //We actually allow more than one . in the key (for example core.schemas.article) in this case the section
                    //is the part up to the last dot and the key is the part after it.
                    var sectionbit = key.Substring(0, key.LastIndexOf("."));
                    var keybit = bits[bits.Length - 1];
                    if (subConfig.ContainsKey(sectionbit))
                    {
                        if (subConfig[sectionbit].ContainsKey(keybit))
                        {
                            return subConfig[sectionbit][keybit];
                        }
                        ex = new Exception(String.Format("Configuration key {0} does not exist in section {1}", keybit, sectionbit));
                    }
                    else
                    {
                        ex = new Exception(String.Format("Configuration section {0} does not exist", sectionbit));
                    }
                }
                else
                {
                    ex = new Exception(String.Format("Configuration key {0} is in the wrong format. It should be in the format [section].[key], for example \"environment.cmsurl\"", key));
                }
            }
            else
            {
                ex = new Exception(String.Format("Configuration for {0} '{1}' does not exist.", global ? "module" : "localization", type));
            }

            Log.Error(ex);
            throw ex;
        }

        public static void Refresh(Localization loc)
        {
            loc.LastSettingsRefresh = DateTime.Now;
        }

        public static void Initialize(List<Dictionary<string,string>> localizationList)
        {
            SetLocalizations(localizationList);
        }

        private static void LoadLocalization(Localization loc)
        {
            //TODO - need to lock something?
            if (_localizationLastRefreshes.ContainsKey(loc.LocalizationId))
            {
                _localizationLastRefreshes[loc.LocalizationId] = DateTime.Now;
            }
            else
            {
                _localizationLastRefreshes.Add(loc.LocalizationId, DateTime.Now);
            }
            var key = loc.LocalizationId;
            {
                var mediaPatterns = new List<string> { "^/favicon.ico" };
                Log.Debug("Loading config for localization : '{0}'", loc.GetBaseUrl());
                var config = new Dictionary<string, Dictionary<string, string>>();
                var url = Path.Combine(loc.Path.ToCombinePath(), SystemFolder, @"config\_all.json").Replace("\\", "/");
                var jsonData = StaticFileManager.Serialize(url, loc, true);
                if (jsonData!=null)
                {
                    //The _all.json file contains a reference to all other configuration files
                    var bootstrapJson = Json.Decode(jsonData);
                    if (bootstrapJson.defaultLocalization != null && bootstrapJson.defaultLocalization)
                    {
                        loc.IsDefaultLocalization = true;
                    }
                    if (bootstrapJson.staging != null && bootstrapJson.staging)
                    {
                        loc.IsStaging = true;
                        Log.Info("Site {0} is a staging site.",loc.GetBaseUrl());
                    }
                    if (bootstrapJson.mediaRoot != null)
                    {
                        string mediaRoot = bootstrapJson.mediaRoot;
                        if (!mediaRoot.EndsWith("/"))
                        {
                            mediaRoot += "/";
                        }
                        Log.Debug("This is site is has media root: " + mediaRoot);
                        mediaPatterns.Add(String.Format("^{0}{1}.*", mediaRoot, mediaRoot.EndsWith("/") ? String.Empty : "/"));
                    }
                    if (IsHtmlDesignPublished)
                    {
                        mediaPatterns.Add(String.Format("^{0}/{1}/assets/.*", loc.Path, SystemFolder));
                    }
                    mediaPatterns.Add(String.Format("^{0}/{1}/.*\\.json$", loc.Path, SystemFolder));
                    foreach (string configUrl in bootstrapJson.files)
                    {
                        var type = configUrl.Substring(configUrl.LastIndexOf("/", StringComparison.Ordinal) + 1);
                        type = type.Substring(0, type.LastIndexOf(".", StringComparison.Ordinal)).ToLower();
                        jsonData = StaticFileManager.Serialize(configUrl, loc, true);
                        if (jsonData!=null)
                        {
                            Log.Debug("Loading config from file: {0} for localization {1}", configUrl, key);
                            config.Add(type, GetConfigFromFile(jsonData));
                        }
                        else
                        {
                            Log.Error("Config file: {0} does not exist for localization {1} - skipping", configUrl, key);
                        }
                    }
                    if (_configuration.ContainsKey(key))
                    {
                        _configuration[key] = config;
                    }
                    else
                    {
                        _configuration.Add(key, config);
                    }
                }
                else
                {
                    Log.Warn("Localization configuration bootstrap file: {0} does not exist for localization {1} - skipping this localization", url, loc.LocalizationId);
                }
                loc.IsHtmlDesignPublished = true;//default
                var versionUrl = Path.Combine(loc.Path.ToCombinePath(), @"version.json").Replace("\\", "/");
                var versionJson = StaticFileManager.Serialize(versionUrl, loc, true);
                if (versionJson == null)
                {
                    //it may be that the version json file is 'unmanaged', ie just placed on the filesystem manually
                    //in which case we try to load it directly - the HTML Design is thus not published from CMS
                    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SystemFolder, @"\assets\version.json");
                    if (File.Exists(path))
                    {
                        versionJson = File.ReadAllText(path);
                        loc.IsHtmlDesignPublished = false;
                    }
                }
                if (versionJson != null)
                {
                    loc.Version = Json.Decode(versionJson).version;
                }
                loc.MediaUrlRegex = String.Join("|", mediaPatterns);
                loc.Culture = GetConfig("core.culture", loc);
                Log.Debug("MediaUrlRegex for localization {0} : {1}", loc.GetBaseUrl(), loc.MediaUrlRegex);
            }
        }

        private static Dictionary<string, string> GetConfigFromFile(string jsonData)
        {
            return new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(jsonData);
        }

        public static string GetPageController()
        {
            return "Page";
        }
        public static string GetPageAction()
        {
            return "Page";
        }

        public static string GetRegionController()
        {
            return "Region";
        }
        public static string GetRegionAction()
        {
            return "Region";
        }

        public static string GetEntityController()
        {
            return "Entity";
        }
        public static string GetEntityAction()
        {
            return "Entity";
        }
        public static string GetDefaultModuleName()
        {
            return "Core";
        }
        
        /// <summary>
        /// The version number used when building paths to HTML Design assets
        /// </summary>
        [Obsolete("Use Version property of current Localization instead. Eg WebRequestContext.Localization.Version.",true)]
        public static string SiteVersion{get;set;}

        /// <summary>
        /// Removes the version number from a URL path for an asset
        /// </summary>
        /// <param name="path">The URL path</param>
        /// <returns>The 'real' path to the asset</returns>
        public static String RemoveVersionFromPath(string path)
        {
            return Regex.Replace(path, SystemFolder + "/" + VersionRegex + "/", delegate
            {
                return SystemFolder + "/";
            });
        }

        /// <summary>
        /// Set the localizations from a List loaded from configuration
        /// </summary>
        /// <param name="localizations">List of configuration data</param>
        public static void SetLocalizations(List<Dictionary<string, string>> localizations)
        {
            Localizations = new Dictionary<string, Localization>();
            foreach (var loc in localizations)
            {
                var localization = new Localization
                {
                    Protocol = !loc.ContainsKey("Protocol") ? "http" : loc["Protocol"],
                    Domain = !loc.ContainsKey("Domain") ? "no-domain-in-cd_link_conf" : loc["Domain"],
                    Port = !loc.ContainsKey("Port") ? String.Empty : loc["Port"],
                    Path = (!loc.ContainsKey("Path") || loc["Path"] == "/") ? String.Empty : loc["Path"],
                    LocalizationId = !loc.ContainsKey("LocalizationId") ? "0" : loc["LocalizationId"]
                };
                Localizations.Add(localization.GetBaseUrl(), localization);
            }
        }

        /// <summary>
        /// Ensure that a URL is using the path to the given localization
        /// </summary>
        /// <param name="url">The URL to localize</param>
        /// <param name="localization">The localization to use</param>
        /// <returns>A localized URL</returns>
        public static string LocalizeUrl(string url, Localization localization)
        {
            if (!String.IsNullOrEmpty(localization.Path))
            {
                return localization.Path + "/" + url;
            }
            return url;
        }

        /// <summary>
        /// Adds a View->View Model Type mapping to the view model registry
        /// </summary>
        /// <param name="viewData">The View Data used to determine the registry key and model type</param>
        /// <param name="viewPath">The path to the view</param>
        public static void AddViewModelToRegistry(MvcData viewData, string viewPath)
        {
            lock (ViewRegistryLock)
            {
                var key = String.Format("{0}:{1}", viewData.AreaName, viewData.ViewName);
                if (!ViewModelRegistry.ContainsKey(key))
                {
                    try
                    {
                        Type type = BuildManager.GetCompiledType(viewPath);
                        if (type.BaseType.IsGenericType)
                        {
                            ViewModelRegistry.Add(key, type.BaseType.GetGenericArguments()[0]);
                        }
                        else
                        {
                            Exception ex = new Exception(String.Format("View {0} is not strongly typed. Please ensure you use the @model directive", viewPath));
                            Log.Error(ex);
                            throw ex;
                        }
                    }
                    catch (Exception ex)
                    {
                        Exception e = new Exception(String.Format("Error adding view model to registry using view path {0}", viewPath), ex);
                        Log.Error(e);
                        throw e;
                    }
                }
            }
        }

        /// <summary>
        /// Generic a GUID
        /// </summary>
        /// <param name="prefix">prefix for the GUID</param>
        /// <returns>Prefixed Unique Identifier</returns>
        public static string GetUniqueId(string prefix)
        {
            return prefix + Guid.NewGuid().ToString("N");
        }

        public static string GetLocalStaticsFolder(string localizationId)
        {
            return string.Format("{0}\\{1}", StaticsFolder, localizationId);
        }
        public static string GetLocalStaticsUrl(string localizationId)
        {
            return GetLocalStaticsFolder(localizationId).Replace("\\","/");
        }

        public static Localization GetLocalizationFromUri(Uri uri)
        {
            string url = uri.ToString();
            foreach (var rootUrl in SiteConfiguration.Localizations.Keys)
            {
                if (url.ToLower().StartsWith(rootUrl.ToLower()))
                {
                    var loc = SiteConfiguration.Localizations[rootUrl];
                    Log.Debug("Request for {0} is from localization {1} ('{2}')", uri, loc.LocalizationId, loc.Path);
                    return loc;
                }
            }
            return null;
        }
    }
}
