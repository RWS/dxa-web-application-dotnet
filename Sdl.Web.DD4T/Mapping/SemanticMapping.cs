using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sdl.Web.Mvc;

namespace Sdl.Web.DD4T.Mapping
{
    /// <summary>
    /// General Semantic Mapping Class which reads schema mapping from json files on disk
    /// </summary>
    public static class SemanticMapping
    {
        public static IStaticFileManager StaticFileManager { get; set; }
        public const string VersionRegex = "(v\\d*.\\d*)";
        public const string SystemFolder = "system";
        public const string CoreModuleName = "core";

        private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> _semanticMap;
        public static Dictionary<string, Dictionary<string, Dictionary<string, string>>> SemanticMap
        {
            get
            {
                if (_semanticMap == null)
                {
                    LoadMapping();
                }
                return _semanticMap;
            }
        }
        private static readonly object MappingLock = new object();

        /// <summary>
        /// Gets a semantic mapping
        /// </summary>
        /// <param name="key">The mapping key, in the format "section.name" (eg "Schema.Article")</param>
        /// <param name="module">The module (eg "Search") - if none specified this defaults to "Core"</param>
        /// <returns>The mapping matching the key for the given module</returns>
        public static string GetMapping(string key, string module = CoreModuleName)
        {
            return GetMapping(SemanticMap, key, module);
        }


        private static void LoadMapping()
        {
            Load(AppDomain.CurrentDomain.BaseDirectory);
        }

        private static string GetMapping(Dictionary<string, Dictionary<string, Dictionary<string, string>>> config, string key, string type)
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
                        ex = new Exception(String.Format("Mapping key {0} does not exist in section {1}", bits[1], bits[0]));
                    }
                    else
                    {
                        ex = new Exception(String.Format("Mapping section {0} does not exist", bits[0]));
                    }
                }
                else
                {
                    ex = new Exception(String.Format("Mapping key {0} is in the wrong format. It should be in the format [section].[key], for example \"environment.cmsurl\"", key));
                }
            }
            else
            {
                ex = new Exception(String.Format("Mapping for module '{0}' does not exist.", type));
            }

            Log.Error(ex);
            throw ex;
        }

        /// <summary>
        /// Loads configuration into memory from database/disk
        /// </summary>
        /// <param name="applicationRoot">The root filepath of the application</param>
        public static void Load(string applicationRoot)
        {
            // we are updating a static variable, so need to be thread safe
            lock (MappingLock)
            {
                // ensure that the config files have been written to disk
                StaticFileManager.CreateStaticAssets(applicationRoot);

                _semanticMap = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            }
        }
    }
}
