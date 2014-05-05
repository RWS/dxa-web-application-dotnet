using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Helpers;
using System.Web.Script.Serialization;

namespace Sdl.Web.Mvc.Mapping
{
    /// <summary>
    /// General Semantic Mapping Class which reads schema mapping from json files on disk
    /// </summary>
    public static class SemanticMapping
    {
        public static IStaticFileManager StaticFileManager { get; set; }

        private static List<SemanticVocabulary> _semanticVocabularies;
        public static List<SemanticVocabulary> SemanticVocabularies
        {
            get
            {
                if (_semanticVocabularies == null)
                {
                    LoadMapping();
                }
                return _semanticVocabularies;
            }
        }

        private static Dictionary<string, List<SemanticSchema>> _semanticMap;
        public static Dictionary<string, List<SemanticSchema>> SemanticMap
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
        /// Gets a semantic schema by id
        /// </summary>
        /// <param name="id">The schema ID</param>
        /// <param name="module">The module (eg "Search") - if none specified this defaults to "Core"</param>
        /// <returns>The semantic schema matching the id for the given module</returns>
        public static SemanticSchema GetSchema(long id, string module = Configuration.CoreModuleName)
        {
            return GetSchema(SemanticMap, id, module);
        }


        private static void LoadMapping()
        {
            Load(AppDomain.CurrentDomain.BaseDirectory);
        }

        private static SemanticSchema GetSchema(Dictionary<string, List<SemanticSchema>> mapping, long id, string type)
        {
            Exception ex;
            if (mapping.ContainsKey(type))
            {
                var list = mapping[type];
                foreach (var semanticSchema in list)
                {
                    if (semanticSchema.Id.Equals(id))
                    {
                        return semanticSchema;
                    }
                }
                ex = new Exception(String.Format("Semantic Schema '{0}' for module '{1}' does not exist.", id, type));
            }
            else
            {
                ex = new Exception(String.Format("Semantic mappings for module '{0}' do not exist.", type));
            }

            Log.Error(ex);
            throw ex;
        }

        /// <summary>
        /// Loads semantic mapping into memory from database/disk
        /// </summary>
        /// <param name="applicationRoot">The root filepath of the application</param>
        public static void Load(string applicationRoot)
        {
            // we are updating a static variable, so need to be thread safe
            lock (MappingLock)
            {
                // ensure that the config files have been written to disk
                StaticFileManager.CreateStaticAssets(applicationRoot);

                _semanticMap = new Dictionary<string, List<SemanticSchema>>();
                _semanticVocabularies = new List<SemanticVocabulary>();

                Log.Debug("Loading config for default localization");
                var path = String.Format("{0}{1}/{2}", applicationRoot, Configuration.DefaultLocalization, Configuration.AddVersionToPath(Configuration.SystemFolder + "/mappings/_all.json"));
                if (File.Exists(path))
                {
                    //The _all.json file contains a reference to all other configuration files
                    Log.Debug("Loading config bootstrap file : '{0}'", path);
                    var bootstrapJson = Json.Decode(File.ReadAllText(path));
                    foreach (string file in bootstrapJson.files)
                    {
                        var type = file.Substring(file.LastIndexOf("/") + 1);
                        type = type.Substring(0, type.LastIndexOf(".")).ToLower();
                        var configPath = applicationRoot + Configuration.AddVersionToPath(file);
                        if (File.Exists(configPath))
                        {
                            Log.Debug("Loading mapping from file: {0}", configPath);
                            if (type.Equals("vocabularies"))
                            {
                                _semanticVocabularies = GetVocabularysFromFile(configPath);
                            }
                            else
                            {
                                var bits = type.Split('.');
                                if (bits[1].Equals("schemas"))
                                {
                                    if (!_semanticMap.ContainsKey(bits[0]))
                                    {
                                        _semanticMap.Add(bits[0], new List<SemanticSchema>());
                                    }
                                    _semanticMap[bits[0]] = GetSchemasFromFile(configPath);                                    
                                }
                            }
                        }
                        else
                        {
                            Log.Error("Config file: {0} does not exist - skipping", configPath);
                        }
                    }
                }
                else
                {
                    Log.Warn("Localization configuration bootstrap file: {0} does not exist - skipping this localization", path);
                }

            }
        }

        private static List<SemanticSchema> GetSchemasFromFile(string file)
        {
            return new JavaScriptSerializer().Deserialize<List<SemanticSchema>>(File.ReadAllText(file));
        }

        private static List<SemanticVocabulary> GetVocabularysFromFile(string file)
        {
            return new JavaScriptSerializer().Deserialize<List<SemanticVocabulary>>(File.ReadAllText(file));
        }
    }
}
