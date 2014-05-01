using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Script.Serialization;

namespace Sdl.Web.Mvc
{
    /// <summary>
    /// General Configuration Class which reads configuration from json files on disk
    /// </summary>
    public static class Configuration
    {
        public static IStaticFileManager StaticFileManager { get; set; }
        public static Dictionary<string, Localization> Localizations { get; set; }
        public const string VERSION_REGEX = "(v\\d*.\\d*)";
        public const string SYSTEM_FOLDER = "system";
        public const string CORE_MODULE_NAME = "core";
        
        private static string _currentVersion = null;
        private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> _localConfiguration;
        private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> _globalConfiguration;
        private static string _defaultLocalization = null;
        public static string DefaultLocalization
        {
            get
            {
                return _defaultLocalization;
            }
        }

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
        private static object configLock = new object();
        
        /// <summary>
        /// Gets a (global) configuration setting
        /// </summary>
        /// <param name="key">The configuration key, in the format "section.name" (eg "Schema.Article")</param>
        /// <param name="module">The module (eg "Search") - if none specified this defaults to "Core"</param>
        /// <returns>The configuration matching the key for the given module</returns>
        public static string GetGlobalConfig(string key, string module = CORE_MODULE_NAME)
        {
            return GetConfig(GlobalConfiguration, key, module, true);
        }

        /// <summary>
        /// Gets a (localized) configuration setting
        /// </summary>
        /// <param name="key">The configuration key, in the format "section.name" (eg "Environment.CmsUrl")</param>
        /// <param name="localization">The localization (eg "en", "fr") - if none specified this is inferred from the request context</param>
        /// <returns>The configuration matching the key for the given localization</returns>
        public static string GetConfig(string key, string localization = null)
        {
            if (localization == null)
            {
                localization = WebRequestContext.Localization.Path;
            }
            return GetConfig(LocalConfiguration, key, localization);
        }

        private static void LoadConfig()
        {
            Load(AppDomain.CurrentDomain.BaseDirectory);
        }
        
        private static string GetConfig(Dictionary<string, Dictionary<string, Dictionary<string, string>>> config, string key, string type, bool global = false)
        {
            Exception ex = null;
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
                    ex = new Exception(String.Format("Configuration section {0} does not exist", bits[0]));
                }
                ex = new Exception(String.Format("Configuration key {0} is in the wrong format. It should be in the format [section].[key], for example \"environment.cmsurl\"", key));
            }
            else
            {
                ex = new Exception(String.Format("Configuration for {0} '{1}' does not exist.", global ? "module" : "localization", type));
            }
            if (ex != null)
            {
                Log.Error(ex);
                throw ex;
            }
            return null;
        }

        /// <summary>
        /// Loads configuration into memory from database/disk
        /// </summary>
        /// <param name="applicationRoot">The root filepath of the application</param>
        public static void Load(string applicationRoot)
        {
            //We are updating a static variable, so need to be thread safe
            lock (configLock)
            {
                //Ensure that the config files have been written to disk
                StaticFileManager.CreateStaticAssets(applicationRoot);

                _localConfiguration = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
                _globalConfiguration = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
                foreach (var loc in Localizations.Values)
                {
                    if (!_localConfiguration.ContainsKey(loc.Path))
                    {
                        Log.Debug("Loading config for localization : '{0}'", loc.Path);
                        var config = new Dictionary<string, Dictionary<string, string>>();
                        var path = String.Format("{0}{1}/{2}", applicationRoot, loc.Path, AddVersionToPath(SYSTEM_FOLDER + "/config/_all.json"));
                        if (File.Exists(path))
                        {
                            //The _all.json file contains a reference to all other configuration files
                            Log.Debug("Loading config bootstrap file : '{0}'", path);
                            var bootstrapJson = Json.Decode(File.ReadAllText(path));
                            if (bootstrapJson.defaultLocalization)
                            {
                                _defaultLocalization = loc.Path;
                                Log.Info("Set default localization : '{0}'", loc.Path);
                            }
                            foreach (string file in bootstrapJson.files)
                            {
                                var type = file.Substring(file.LastIndexOf("/") + 1);
                                type = type.Substring(0, type.LastIndexOf(".")).ToLower();
                                var configPath = applicationRoot + AddVersionToPath(file);
                                if (File.Exists(configPath))
                                {
                                    Log.Debug("Loading config from file: {0}", configPath);
                                    //For the default localization we load in global configuration
                                    if (type.Contains(".") && loc.Path==_defaultLocalization)
                                    {
                                        var bits = type.Split('.');
                                        if (!_globalConfiguration.ContainsKey(bits[0]))
                                        {
                                            _globalConfiguration.Add(bits[0], new Dictionary<string, Dictionary<string, string>>());
                                        }
                                        _globalConfiguration[bits[0]].Add(bits[1], GetConfigFromFile(configPath));
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
                            _localConfiguration.Add(loc.Path, config);
                        }
                        else
                        {
                            Log.Warn("Localization configuration bootstrap file: {0} does not exist - skipping this localization", path);
                        }
                    }
                }
                //Filter out localizations that were not found on disk, and add culture/set default localization from config
                Dictionary<string, Localization> relevantLocalizations = new Dictionary<string, Localization>();
                foreach (var loc in Localizations)
                {
                    if (_localConfiguration.ContainsKey(loc.Value.Path))
                    {
                        var config = _localConfiguration[loc.Value.Path];
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
                Log.Debug("The following localizations are active for this site: {0}", String.Join(", ", Localizations.Select(l=>l.Key).ToArray()));
            }            
        }

        private static Dictionary<string, string> GetConfigFromFile(string file)
        {
            return new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(File.ReadAllText(file));
        }

        public static string GetDefaultPageName()
        {
            return "index.html";
        }
        public static string GetDefaultExtension()
        {
            return ".html";
        }
        public static string GetRegionController()
        {
            return "Region";
        }
        public static string GetRegionAction()
        {
            return "Region";
        }
        public static string GetCmsUrl()
        {
            return GetConfig("environment.cmsurl");
        }
        
        public static String AddVersionToPath(string path)
        {
            return path.Replace(SYSTEM_FOLDER + "/", String.Format("{0}/{1}/", SYSTEM_FOLDER, CurrentVersion));
        }

        public static String RemoveVersionFromPath(string path)
        {
            return Regex.Replace(path, SYSTEM_FOLDER + "/" + VERSION_REGEX + "/", delegate(Match match)
            {
                return SYSTEM_FOLDER + "/";
            });
        }

        public static string SiteVersion
        {
            get
            {
                return ConfigurationManager.AppSettings["Sdl.Web.SiteVersion"];
            }
        }

        public static string CurrentVersion
        {
            get
            {
                if (_currentVersion == null)
                {
                    //The current version is the the latest version that exists on disk in one or more localizations
                    //UNLESS an earlier version is specified in web.config (Sdl.Web.SiteVersion) for rollback purposes
                    //When a new version is serialized to disk the current version will also be updated accordingly
                    foreach (var loc in Localizations.Values)
                    {
                        DirectoryInfo di = new DirectoryInfo(String.Format("{0}{1}/{2}", AppDomain.CurrentDomain.BaseDirectory, loc.Path, SYSTEM_FOLDER));
                        if (di.Exists)
                        {
                            foreach (DirectoryInfo dir in di.GetDirectories("v*"))
                            {
                                if (_currentVersion == null || dir.Name.CompareTo(_currentVersion) > 0)
                                {
                                    _currentVersion = dir.Name;
                                }
                            }
                        }
                    }
                    if (_currentVersion == null || _currentVersion.CompareTo(SiteVersion) > 0)
                    {
                        _currentVersion = SiteVersion;
                    }
                }
                return _currentVersion;
            }
            set
            {
                _currentVersion = value;
            }
        }

        public static void SetLocalizations(List<Dictionary<string, string>> localizations)
        {
            Localizations = new Dictionary<string, Localization>();
            foreach (var loc in localizations)
            {
                var localization = new Localization();
                localization.Protocol = !loc.ContainsKey("Protocol") ? "http" : loc["Protocol"];
                localization.Domain = !loc.ContainsKey("Domain") ? "no-domain-in-cd_link_conf" : loc["Domain"];
                localization.Port = !loc.ContainsKey("Port") ? "" : loc["Port"];
                localization.Path = (!loc.ContainsKey("Path") || loc["Path"] == "/") ? "" : loc["Path"];
                localization.LocalizationId = !loc.ContainsKey("LocalizationId") ? 0 : Int32.Parse(loc["LocalizationId"]);
                Localizations.Add(localization.GetBaseUrl(), localization);
            }
        }

        public static string LocalizeUrl(string url)
        {
            if (!String.IsNullOrEmpty(DefaultLocalization))
            {
                return DefaultLocalization + "/" + url;
            }
            return url;
        }

    }
}
