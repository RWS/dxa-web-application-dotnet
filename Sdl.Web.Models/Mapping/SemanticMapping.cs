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
        /// Gets semantic vocabulary by prefix
        /// </summary>
        /// <param name="prefix">the prefix</param>
        /// <returns>semantic vocabulary for the given prefix</returns>
        public static string GetVocabulary(string prefix)
        {
            return GetVocabulary(SemanticVocabularies, prefix);
        }

        private static string GetVocabulary(List<SemanticVocabulary> vocabularies, string prefix)
        {
            SemanticVocabulary vocabulary = vocabularies.Find(v => v.Prefix.Equals(prefix));
            if (vocabulary != null)
            {
                return vocabulary.Vocab;
            }

            Exception ex = new Exception(string.Format("Semantic Vocabulary not found for prefix '{0}'", prefix));
            Log.Error(ex);
            throw ex;
        }

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

        private static SemanticSchema GetSchema(IReadOnlyDictionary<string, List<SemanticSchema>> mapping, long id, string type)
        {
            Exception ex;
            if (mapping.ContainsKey(type))
            {
                var semanticSchemaList = mapping[type];
                SemanticSchema schema = semanticSchemaList.Find(s => s.Id.Equals(id));
                if (schema != null)
                {
                    return schema;
                }
                ex = new Exception(string.Format("Semantic Schema '{0}' for module '{1}' does not exist.", id, type));
            }
            else
            {
                ex = new Exception(string.Format("Semantic mappings for module '{0}' do not exist.", type));
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
                Configuration.StaticFileManager.CreateStaticAssets(applicationRoot);

                _semanticMap = new Dictionary<string, List<SemanticSchema>>();
                _semanticVocabularies = new List<SemanticVocabulary>();

                Log.Debug("Loading semantic mappings for default localization");
                var path = String.Format("{0}{1}/{2}", applicationRoot, Configuration.DefaultLocalization, Configuration.AddVersionToPath(Configuration.SystemFolder + "/mappings/_all.json"));
                if (File.Exists(path))
                {
                    // the _all.json file contains a reference to all other configuration files
                    Log.Debug("Loading semantic mapping bootstrap file : '{0}'", path);
                    var bootstrapJson = Json.Decode(File.ReadAllText(path));
                    foreach (string file in bootstrapJson.files)
                    {
                        var type = file.Substring(file.LastIndexOf("/", StringComparison.Ordinal) + 1);
                        type = type.Substring(0, type.LastIndexOf(".", StringComparison.Ordinal)).ToLower();
                        var configPath = applicationRoot + Configuration.AddVersionToPath(file);
                        if (File.Exists(configPath))
                        {
                            Log.Debug("Loading mapping from file: {0}", configPath);
                            if (type.Equals("vocabularies"))
                            {
                                _semanticVocabularies = GetVocabulariesFromFile(configPath);
                            }
                            else
                            {
                                var bits = type.Split('.');
                                // we are only interested in schemas at the moment
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
                            Log.Error("Semantic mapping file: {0} does not exist - skipping", configPath);
                        }
                    }
                }
                else
                {
                    Log.Warn("Semantic mapping bootstrap file: {0} does not exist - skipping this", path);
                }

            }
        }

        private static List<SemanticSchema> GetSchemasFromFile(string file)
        {
            return new JavaScriptSerializer().Deserialize<List<SemanticSchema>>(File.ReadAllText(file));
        }

        private static List<SemanticVocabulary> GetVocabulariesFromFile(string file)
        {
            return new JavaScriptSerializer().Deserialize<List<SemanticVocabulary>>(File.ReadAllText(file));
        }
    }
}
