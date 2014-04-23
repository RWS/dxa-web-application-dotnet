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
        public static string VERSION_REGEX = "(v\\d*.\\d*)";
        public static string SYSTEM_FOLDER = "system";
        
        private static string _currentVersion = null;
        private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> _configuration;
        private static object configLock = new object();
        
        /// <summary>
        /// Gets a (localized) configuration setting
        /// </summary>
        /// <param name="key">The configuration key, in the format "section.name" (eg "environment.cmsurl")</param>
        /// <param name="localization">The localization (eg "en", "fr") - if none specified this is inferred from the request context</param>
        /// <returns>The configuration matching the key for the given localization</returns>
        public static string GetConfig(string key, string localization = null)
        {
            if (_configuration == null)
            {
                Load(AppDomain.CurrentDomain.BaseDirectory);
            }
            if (localization == null)
            {
                localization = WebRequestContext.Localization.Path;
            }
            if (_configuration.ContainsKey(localization))
            {
                var config = _configuration[localization];
                var bits = key.Split('.');
                if (bits.Length == 2)
                {
                    if (config.ContainsKey(bits[0]))
                    {
                        if (config[bits[0]].ContainsKey(bits[1]))
                        {
                            return config[bits[0]][bits[1]];
                        }
                        throw new Exception(String.Format("Configuration key {0} does not exist in section {1}", bits[1], bits[0]));
                    }
                    throw new Exception(String.Format("Configuration section {0} does not exist", bits[0]));
                }
                throw new Exception(String.Format("Configuration key {0} is in the wrong format. It should be in the format [section].[key], for example \"environment.cmsurl\"", key));
            }
            else
            {
                throw new Exception(String.Format("Configuration localization '{0}' does not exist.",localization));
            }
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
                _configuration = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
                foreach (var loc in Localizations.Values)
                {
                    if (!_configuration.ContainsKey(loc.Path))
                    {
                        var config = new Dictionary<string, Dictionary<string, string>>();
                        var path = String.Format("{0}{1}/{2}", applicationRoot, loc.Path, AddVersionToPath(SYSTEM_FOLDER + "/config/_all.json"));
                        if (File.Exists(path))
                        {
                            //The _all.json file contains a reference to all other configuration files
                            var bootstrapJson = Json.Decode(File.ReadAllText(path));
                            foreach (string file in bootstrapJson.files)
                            {
                                var type = file.Substring(file.LastIndexOf("/") + 1);
                                type = type.Substring(0, type.LastIndexOf(".")).ToLower();
                                var configPath = applicationRoot + AddVersionToPath(file);
                                if (File.Exists(configPath))
                                {
                                    config.Add(type, GetConfigFromFile(configPath));
                                }
                                else
                                {
                                    //TODO log a warning, or throw an error?!
                                }
                            }
                            _configuration.Add(loc.Path, config);
                        }
                        else
                        {
                            //TODO log a warning, although this should not be an error, as it is quite possible that localizations are configured
                            //for which no configuration has yet been published
                        }
                    }
                }
                //Filter out localizations that were not found on disk, and add culture from config
                Dictionary<string, Localization> relevantLocalizations = new Dictionary<string, Localization>();
                foreach (var loc in Localizations)
                {
                    if (_configuration.ContainsKey(loc.Value.Path))
                    {
                        var config = _configuration[loc.Value.Path];
                        if (config.ContainsKey("site") && config["site"].ContainsKey("culture"))
                        {
                            loc.Value.Culture = config["site"]["culture"];
                        }
                        relevantLocalizations.Add(loc.Key, loc.Value);
                    }
                }
                Localizations = relevantLocalizations;
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
    }
}
