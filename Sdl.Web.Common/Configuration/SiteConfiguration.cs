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

        private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> _localConfiguration;
        private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> _globalConfiguration;        
        private static Dictionary<string, Type> _viewModelRegistry;
        
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
        /// For multi-localization (language) websites, one is set to be the default
        /// </summary>
        public static string DefaultLocalization { get; private set; }

        /// <summary>
        /// True if this is a staging website
        /// </summary>
        public static bool IsStaging { get; set; }

        /// <summary>
        /// A dictionary of local (varying per localization) configuration settings, typically accessed with the GetConfig method, or Html.Config extension method (in Views)
        /// </summary> 
        public static Dictionary<string, Dictionary<string, Dictionary<string, string>>> LocalConfiguration
        {
            get
            {
                if (_localConfiguration == null)
                {
                    LoadConfig();
                }
                return _localConfiguration;
            }
        }

        /// <summary>
        /// A dictionary of global (not varying per localization) configuration settings, typically accessed with the GetGlobalConfig method
        /// </summary> 
        public static Dictionary<string, Dictionary<string, Dictionary<string, string>>> GlobalConfiguration
        {
            get
            {
                if (_globalConfiguration == null)
                {
                    LoadConfig();
                }
                return _globalConfiguration;
            }
        }

        public static DateTime LastSettingsRefresh { get; set; }

        /// <summary>
        /// Used to determine which URLs should be processed by the BinaryDistributionModule
        /// </summary>
        public static string MediaUrlRegex { get; set; }

        private static readonly object ConfigLock = new object();
        private static readonly object ViewRegistryLock = new object();
        
        /// <summary>
        /// Gets a (global) configuration setting
        /// </summary>
        /// <param name="key">The configuration key, in the format "section.name" (eg "Schema.Article")</param>
        /// <param name="module">The module (eg "Search") - if none specified this defaults to "Core"</param>
        /// <returns>The configuration matching the key for the given module</returns>
        public static string GetGlobalConfig(string key, string module = CoreModuleName)
        {
            return GetConfig(GlobalConfiguration, key, module, true);
        }

        /// <summary>
        /// Gets a (localized) configuration setting
        /// </summary>
        /// <param name="key">The configuration key, in the format "section.name" (eg "Environment.CmsUrl")</param>
        /// <param name="localization">The localization (eg "en", "fr") - if none specified the default is used</param>
        /// <returns>The configuration matching the key for the given localization</returns>
        public static string GetConfig(string key, string localization = null)
        {
            if (localization == null)
            {
                localization = DefaultLocalization;
            }
            return GetConfig(LocalConfiguration, key, localization);
        }

        private static void LoadConfig()
        {
            Load(AppDomain.CurrentDomain.BaseDirectory);
        }
        
        private static string GetConfig(IReadOnlyDictionary<string, Dictionary<string, Dictionary<string, string>>> config, string key, string type, bool global = false)
        {
            Exception ex;
            if (config.ContainsKey(type))
            {
                var subConfig = config[type];
                var bits = key.Split('.');
                if (bits.Length == 2)
                {
                    if (subConfig.ContainsKey(bits[0]))
                    {
                        if (subConfig[bits[0]].ContainsKey(bits[1]))
                        {
                            return subConfig[bits[0]][bits[1]];
                        }
                        ex = new Exception(String.Format("Configuration key {0} does not exist in section {1}", bits[1], bits[0]));
                    }
                    else
                    {
                        ex = new Exception(String.Format("Configuration section {0} does not exist", bits[0]));
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

        public static void Refresh()
        {
            LastSettingsRefresh = DateTime.Now;
            var appRoot = AppDomain.CurrentDomain.BaseDirectory;
            Load(appRoot);
            SemanticMapping.Load(appRoot);
        }

        public static void Initialize(List<Dictionary<string,string>> localizationList)
        {
            SetLocalizations(localizationList);
            Refresh();
        }
            
        /// <summary>
        /// Loads configuration into memory from database/disk
        /// </summary>
        /// <param name="applicationRoot">The root filepath of the application</param>
        public static void Load(string applicationRoot)
        {
            //We are updating a static variable, so need to be thread safe
            lock (ConfigLock)
            {
                //Ensure that the config files have been written to disk and HTML Design version is 
                var version = StaticFileManager.CreateStaticAssets(applicationRoot) ?? DefaultVersion;
                SiteVersion = version;
                var mediaPatterns = new List<string>{"^/favicon.ico"};
                var localConfiguration = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
                var globalConfiguration = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
                foreach (var loc in Localizations.Values)
                {
                    if (!localConfiguration.ContainsKey(loc.Path))
                    {
                        Log.Debug("Loading config for localization : '{0}'", loc.Path);
                        var config = new Dictionary<string, Dictionary<string, string>>();
                        var path = Path.Combine(new[] { applicationRoot, StaticsFolder, loc.Path.ToCombinePath(), SystemFolder, @"config\_all.json" });
                        if (File.Exists(path))
                        {
                            //The _all.json file contains a reference to all other configuration files
                            Log.Debug("Loading config bootstrap file : '{0}'", path);
                            var bootstrapJson = Json.Decode(File.ReadAllText(path));
                            if (bootstrapJson.defaultLocalization!=null && bootstrapJson.defaultLocalization)
                            {
                                DefaultLocalization = loc.Path;
                                Log.Info("Set default localization : '{0}'", loc.Path);
                            }
                            if (bootstrapJson.staging != null && bootstrapJson.staging)
                            {
                                IsStaging = true;
                                Log.Info("This is site is staging");
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
                            mediaPatterns.Add(String.Format("^{0}/{1}/.*\\.json$",loc.Path, SystemFolder));
                            foreach (string file in bootstrapJson.files)
                            {
                                var type = file.Substring(file.LastIndexOf("/", StringComparison.Ordinal) + 1);
                                type = type.Substring(0, type.LastIndexOf(".", StringComparison.Ordinal)).ToLower();
                                var configPath = Path.Combine(new[] { applicationRoot, StaticsFolder, file.ToCombinePath() });
                                if (File.Exists(configPath))
                                {
                                    Log.Debug("Loading config from file: {0}", configPath);
                                    //For the default localization we load in global configuration
                                    if (type.Contains(".") && loc.Path==DefaultLocalization)
                                    {
                                        var bits = type.Split('.');
                                        if (!globalConfiguration.ContainsKey(bits[0]))
                                        {
                                            globalConfiguration.Add(bits[0], new Dictionary<string, Dictionary<string, string>>());
                                        }
                                        globalConfiguration[bits[0]].Add(bits[1], GetConfigFromFile(configPath));
                                    }
                                    else
                                    {
                                        config.Add(type, GetConfigFromFile(configPath));
                                    }
                                }
                                else
                                {
                                    Log.Error("Config file: {0} does not exist - skipping", configPath);
                                }
                            }
                            localConfiguration.Add(loc.Path, config);
                        }
                        else
                        {
                            Log.Warn("Localization configuration bootstrap file: {0} does not exist - skipping this localization", path);
                        }
                    }
                }
                MediaUrlRegex = String.Join("|", mediaPatterns);
                Log.Debug("MediaUrlRegex: " + MediaUrlRegex);
                //Filter out localizations that were not found on disk, and add culture/set default localization from config
                Dictionary<string, Localization> relevantLocalizations = new Dictionary<string, Localization>();
                foreach (var loc in Localizations)
                {
                    if (localConfiguration.ContainsKey(loc.Value.Path))
                    {
                        var config = localConfiguration[loc.Value.Path];
                        if (config.ContainsKey("site"))
                        {
                            if (config["site"].ContainsKey("culture"))
                            {
                                loc.Value.Culture = config["site"]["culture"];
                            }
                        }
                        relevantLocalizations.Add(loc.Key, loc.Value);
                    }
                }
                Localizations = relevantLocalizations;
                _localConfiguration = localConfiguration;
                _globalConfiguration = globalConfiguration;
                Log.Debug("The following localizations are active for this site: {0}", String.Join(", ", Localizations.Select(l=>l.Key).ToArray()));
            }            
        }

        private static Dictionary<string, string> GetConfigFromFile(string file)
        {
            return new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(File.ReadAllText(file));
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
        public static string LocalizeUrl(string url, Localization localization = null)
        {
            string path = (localization==null) ? DefaultLocalization : localization.Path;
            if (!String.IsNullOrEmpty(path))
            {
                return path + "/" + url;
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
    }
}
