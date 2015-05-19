using System.Web.UI.WebControls;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Compilation;
using System.Web.Helpers;
using System.Web.Script.Serialization;

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
        /// Localization resolver used for mapping URLs to localizations/content stores
        /// </summary>
        public static ILocalizationManager LocalizationManager { get; set; }

        public const string VersionRegex = "(v\\d*.\\d*)";
        public const string SystemFolder = "system";
        public const string CoreModuleName = "core";
        public const string StaticsFolder = "BinaryData";
        public const string DefaultVersion = "v1.00";

        private static readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> Configuration = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

        private static readonly object LocalizationUpdateLock = new object();
        private const string SettingsType = "config";

        /// <summary>
        /// A registry of View Path -> View Model Type mappings to enable the correct View Model to be mapped for a given View
        /// </summary>
        [Obsolete("Dropped in DXA 1.1. Use ModelTypeRegistry.GetViewModelType instead.")]
        public static Dictionary<string, Type> ViewModelRegistry
        {
            get
            {
                throw new NotSupportedException("SiteConfiguration.ViewModelRegistry is dropped in DXA 1.1. Use ModelTypeRegistry.GetViewModelType instead.");
            }
            set
            {
                throw new NotSupportedException("SiteConfiguration.ViewModelRegistry is dropped in DXA 1.1.");
            }
        }

        /// <summary>
        /// Gets a (localized) configuration setting
        /// </summary>
        /// <param name="key">The configuration key, in the format "section.name" (eg "Environment.CmsUrl")</param>
        /// <param name="localization">The localization to get config for</param>
        /// <returns>The configuration matching the key for the given localization</returns>
        public static string GetConfig(string key, Localization localization = null)
        {
            if (localization == null)
            {
                localization = LocalizationManager.GetContextLocalization();
            }
            if (!CheckConfig(localization.LocalizationId))
            {
                LocalizationManager.UpdateLocalization(localization, true);
            }
            return GetConfig(Configuration, key, localization.LocalizationId);
        }

        private static bool CheckConfig(string localizationId)
        {
            if (!Configuration.ContainsKey(localizationId) || CheckSettingsNeedRefresh(SettingsType, localizationId))
            {
                return false;
            }
            return true;
        }
        
        private static string GetConfig(IReadOnlyDictionary<string, Dictionary<string, Dictionary<string, string>>> config, string key, string type, bool global = false)
        {
            string error;
            if (config.ContainsKey(type))
            {
                var subConfig = config[type];
                var bits = key.Split('.');
                if (bits.Length >= 2)
                {
                    //We actually allow more than one . in the key (for example core.schemas.article) in this case the section
                    //is the part up to the last dot and the key is the part after it.
                    var sectionbit = key.Substring(0, key.LastIndexOf(".", StringComparison.Ordinal));
                    var keybit = bits[bits.Length - 1];
                    if (subConfig.ContainsKey(sectionbit))
                    {
                        if (subConfig[sectionbit].ContainsKey(keybit))
                        {
                            return subConfig[sectionbit][keybit];
                        }
                        error = String.Format("Configuration key {0} does not exist in section {1}", keybit, sectionbit);
                    }
                    else
                    {
                        error = String.Format("Configuration section {0} does not exist", sectionbit);
                    }
                }
                else
                {
                    error = String.Format("Configuration key {0} is in the wrong format. It should be in the format [section].[key], for example \"environment.cmsurl\"", key);
                }
            }
            else
            {
                error = String.Format("Configuration for {0} '{1}' does not exist.", global ? "module" : "localization", type);
            }
            Log.Error(error);
            return null;
        }

        public static void Refresh(Localization loc)
        {
            lock(LocalizationUpdateLock)
            {
                //refresh all localizations for this site
                foreach(var localization in loc.SiteLocalizations)
                {
                    LocalizationManager.UpdateLocalization(localization);
                }
            }
        }

        private static void LoadLocalizationDetails(Localization loc, IEnumerable<string> fileUrls)
        {
            var key = loc.LocalizationId;
            var config = new Dictionary<string, Dictionary<string, string>>();
            foreach (string configUrl in fileUrls)
            {
                var type = configUrl.Substring(configUrl.LastIndexOf("/", StringComparison.Ordinal) + 1);
                type = type.Substring(0, type.LastIndexOf(".", StringComparison.Ordinal)).ToLower();
                var jsonData = StaticFileManager.Serialize(configUrl, loc, true);
                if (jsonData != null)
                {
                    Log.Debug("Loading config from file: {0} for localization {1}", configUrl, key);
                    config.Add(type, GetConfigFromFile(jsonData));
                }
                else
                {
                    Log.Error("Config file: {0} does not exist for localization {1} - skipping", configUrl, key);
                }
            }
            ThreadSafeSettingsUpdate(SettingsType, Configuration, key, config);
        }

        public static Localization LoadLocalization(Localization loc, bool loadDetails = false)
        {
            var key = loc.LocalizationId;
            Log.Debug("Loading config for localization : {0}", key);
            var localization = new Localization
            {
                Path = loc.Path,
                LocalizationId = loc.LocalizationId,
                IsHtmlDesignPublished = true
            };
            var mediaPatterns = new List<string>();
            var versionUrl = Path.Combine(loc.Path.ToCombinePath(true), @"version.json").Replace("\\", "/");
            var versionJson = StaticFileManager.Serialize(versionUrl, loc, true);
            if (versionJson == null)
            {
                //it may be that the version json file is 'unmanaged', ie just placed on the filesystem manually
                //in which case we try to load it directly - the HTML Design is thus not published from CMS
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SystemFolder, @"assets\version.json");
                if (File.Exists(path))
                {
                    versionJson = File.ReadAllText(path);
                    localization.IsHtmlDesignPublished = false;
                }
            }
            if (versionJson != null)
            {
                localization.Version = Json.Decode(versionJson).version;
            }
            var bootstrapJson = GetConfigBootstrapJson(loc);
            if (bootstrapJson != null)
            {
                //The _all.json file contains a reference to all other configuration files
                if (bootstrapJson.defaultLocalization != null && bootstrapJson.defaultLocalization)
                {
                    localization.IsDefaultLocalization = true;
                }
                if (bootstrapJson.staging != null && bootstrapJson.staging)
                {
                    localization.IsStaging = true;
                    Log.Info("Localization {0} is a staging site.",loc.LocalizationId);
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
                if (bootstrapJson.siteLocalizations != null)
                {
                    localization.SiteLocalizations = new List<Localization>();
                    foreach (var item in bootstrapJson.siteLocalizations)
                    {
                        localization.SiteLocalizations.Add(new Localization { LocalizationId = item.id ?? item, Path = item.path, Language = item.language, IsDefaultLocalization = item.isMaster ?? false });
                    }
                }
                if (localization.IsHtmlDesignPublished)
                {
                    mediaPatterns.Add("^/favicon.ico");
                    mediaPatterns.Add(String.Format("^{0}/{1}/assets/.*", loc.Path, SystemFolder));
                }
                if (bootstrapJson.files != null && loadDetails)
                {
                    List<string> configFiles = new List<string>();
                    foreach (string file in bootstrapJson.files)
                    {
                        configFiles.Add(file);
                    }
                    LoadLocalizationDetails(loc, configFiles);
                }
                mediaPatterns.Add(String.Format("^{0}/{1}/.*\\.json$", loc.Path, SystemFolder));
            }
            localization.MediaUrlRegex = String.Join("|", mediaPatterns);
            localization.Culture = GetConfig("core.culture", loc);
            localization.Language = GetConfig("core.language", loc);
            var formats = GetConfig("core.dataFormats", loc);
            localization.DataFormats = formats == null ? new List<string>() : formats.Split(',').Select(f => f.Trim()).ToList();
            Log.Debug("MediaUrlRegex for localization {0} : {1}", localization.LocalizationId, localization.MediaUrlRegex);
            return localization;
        }

        private static dynamic GetConfigBootstrapJson(Localization loc)
        {
            var url = Path.Combine(loc.Path.ToCombinePath(true), SystemFolder, @"config\_all.json").Replace("\\", "/");
            var jsonData = StaticFileManager.Serialize(url, loc, true);
            if (jsonData != null)
            {
                return Json.Decode(jsonData);
            }

            var ex = new Exception(String.Format("Could not load configuration bootstrap file {0} for localization {1}.", url, loc.LocalizationId));
            Log.Error(ex);
            throw ex;
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
        /// Adds a View->View Model Type mapping to the view model registry
        /// </summary>
        /// <param name="viewData">The View Data used to determine the registry key and model type</param>
        /// <param name="viewPath">The path to the view</param>
        [Obsolete("Method is deprecated in DXA 1.1. Use BaseAreaRegistration.RegisterViewModel instead.")]
        public static void AddViewModelToRegistry(MvcData viewData, string viewPath)
        {
            ModelTypeRegistry.RegisterViewModel(viewData, viewPath);
        }

        [Obsolete("Method is deprecated in DXA 1.1. Use ModelTypeRegistry instead.")]
        public static string GetViewModelRegistryKey(MvcData mvcData)
        {
            return String.Format("{0}:{1}:{2}", mvcData.AreaName, mvcData.ControllerName, mvcData.ViewName);
        }

        [Obsolete("Method is deprecated in DXA 1.1. Use BaseAreaRegistration.RegisterViewModel instead.")]
        public static void AddViewModelToRegistry(MvcData mvcData, Type modelType)
        {
            ModelTypeRegistry.RegisterViewModel(mvcData, modelType);
        }

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
        /// Take a partial URL (so not including protocol, domain, port) and make it full by
        /// Adding the protocol, domain, port etc. from the given localization
        /// </summary>
        public static string MakeFullUrl(string url, Localization loc)
        {
            if (url.StartsWith(loc.Path))
            {
                url = url.Substring(loc.Path.Length);
            }
            return url.StartsWith("http") ? url : loc.GetBaseUrl() + url;
        }
        
        public static string GetLocalStaticsFolder(string localizationId)
        {
            return string.Format("{0}\\{1}", StaticsFolder, localizationId);
        }

        public static string GetLocalStaticsUrl(string localizationId)
        {
            return GetLocalStaticsFolder(localizationId).Replace("\\","/");
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
        
        #region Thread Safe Settings Update Helper Methods
        
        //A set of refresh states, keyed by localization id and then type (eg "config", "resources" etc.) 
        private static readonly Dictionary<string, Dictionary<string, DateTime>> RefreshStates = new Dictionary<string, Dictionary<string, DateTime>>();
        //A set of locks to use, one per localization
        private static readonly Dictionary<string, object> LocalizationLocks = new Dictionary<string, object>();
        //A global lock
        private static readonly object Lock = new object();
        
        public static bool CheckSettingsNeedRefresh(string type, string localizationId)
        {
            if (RefreshStates.ContainsKey(localizationId))
            {
                return RefreshStates[localizationId].ContainsKey(type) && RefreshStates[localizationId][type] < LocalizationManager.GetLastLocalizationRefresh(localizationId);
            }
            return false;
        }

        public static void ThreadSafeSettingsUpdate<T>(string type, Dictionary<string,T> settings, string localizationId, T value)
        {
            lock (GetLocalizationLock(localizationId))
            {
                settings[localizationId] = value;
                UpdateRefreshState(localizationId, type);
            }
        }

        private static void UpdateRefreshState(string localizationId, string type)
        {
            //Update is already done under a localization lock, so we don't need to lock again here
            if (!RefreshStates.ContainsKey(localizationId))
            {
                RefreshStates.Add(localizationId, new Dictionary<string, DateTime>());
            }
            var states = RefreshStates[localizationId];
            if (states.ContainsKey(type))
            {
                states[type] = DateTime.Now;
            }
            else
            {
                states.Add(type, DateTime.Now);
            }
        }

        private static object GetLocalizationLock(string localizationId)
        {
            if (!LocalizationLocks.ContainsKey(localizationId))
            {
                lock (Lock)
                {
                    LocalizationLocks.Add(localizationId, new object());
                }
            }
            return LocalizationLocks[localizationId];
        }

        #endregion

        #region Deprecated Methods
        [Obsolete("Use Localization.IsStaging property of current localization (eg via WebRequestContext.Localization.IsStaging)", true)]
        public static bool IsStaging { get; set; }
        
        [Obsolete("There is no longer the concept of a global default localization. The Localization.IsDefaultLocalization property can help you find if a localization is the default for its site.", true)]
        public static string DefaultLocalization { get; private set; }

        [Obsolete("Configuration should not be access directly, but rather via the GetConfig(string, Localization) method.", true)]
        public static Dictionary<string, Dictionary<string, Dictionary<string, string>>> LocalConfiguration { get; set; }

        [Obsolete("Configuration should not be access directly, but rather via the GetConfig(string, Localization) method. There is also no longer a concept of Global Configuration, all configuration is local to a particular localization.", true)]
        public static Dictionary<string, Dictionary<string, Dictionary<string, string>>> GlobalConfiguration { get; set; }

        [Obsolete("Settings refresh is now applied at a localization level, rather than globally", true)]
        public static DateTime LastSettingsRefresh { get; set; }

        [Obsolete("Use Localization.MediaUrlRegex property of current localization (eg via WebRequestContext.Localization.IsStaging)", true)]
        public static string MediaUrlRegex { get; set; }

        [Obsolete("GetGlobalConfig(string,string) is deprecated, please use GetConfig(string, Localization) by combining the key and module parameters in the format {module.key} (eg core.schemas.article)", true)]
        public static string GetGlobalConfig(string key, string module = CoreModuleName)
        {
            return null;
        }

        [Obsolete("Use Version property of current Localization instead. Eg WebRequestContext.Localization.Version.", true)]
        public static string SiteVersion { get; set; }

        [Obsolete("Use Refresh(Localization) - settings refresh can no longer be applied globally, only for a particular Localization at a time", true)]
        public static void Refresh()
        {
        }

        [Obsolete("Configuration is now lazy loaded on demand per localization, so there is no need to call Load.", true)]
        public static void Load(string applicationRoot)
        {
        }

        [Obsolete("Localizations are now loaded on demand in the web application so this is no longer required", true)]
        public static void SetLocalizations(List<Dictionary<string, string>> localizations)
        {
        }

        [Obsolete("Localizations are now loaded on demand in the web application so this is no longer required", true)]
        public static void Initialize(List<Dictionary<string, string>> localizationList)
        {            
        }

        [Obsolete("Localizations are now loaded on demand in the web application so this is no longer available. Use the SiteConfiguration.LocalizationResolver.GetLocalizationByUri or GetLocalizationById methods", true)]
        public static Dictionary<string, Localization> Localizations { get; set; }        
        #endregion
    }
}
