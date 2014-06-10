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
        /// <summary>
        /// Default semantic vocabulary prefix.
        /// </summary>
        public const string DefaultPrefix = "tsi";

        /// <summary>
        /// Default semantic vocabulary.
        /// </summary>
        public const string DefaultVocabulary = "http://www.sdl.com/web/schemas/core";

        /// <summary>
        /// List of semantic vocabularies (prefix and name).
        /// </summary>
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

        /// <summary>
        /// Dictionary with semantic schema mappings, indexed by schema identifier.
        /// </summary>
        public static Dictionary<string, SemanticSchema> SemanticMap
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

        public static Dictionary<string, XpmRegion> XpmRegions
        {
            get
            {
                if (_xpmRegions == null)
                {
                    LoadMapping();
                }
                return _xpmRegions;
            }
        }

        private static Dictionary<string, SemanticSchema> _semanticMap;
        private static List<SemanticVocabulary> _semanticVocabularies;
        private static Dictionary<string, XpmRegion> _xpmRegions; 
        private static readonly object MappingLock = new object();

        /// <summary>
        /// Gets prefix for semantic vocabulary.
        /// </summary>
        /// <param name="vocab">Vocabulary name</param>
        /// <returns>Prefix for this semantic vocabulary</returns>
        public static string GetPrefix(string vocab)
        {
            return GetPrefix(SemanticVocabularies, vocab);
        }

        private static string GetPrefix(List<SemanticVocabulary> vocabularies, string vocab)
        {
            SemanticVocabulary vocabulary = vocabularies.Find(v => v.Vocab.Equals(vocab));
            if (vocabulary != null)
            {
                return vocabulary.Prefix;
            }
            else
            {
                Log.Warn("Prefix not found for semantic vocabulary '{0}'", vocab);
            }
            return null;
        }

        /// <summary>
        /// Gets semantic vocabulary by prefix.
        /// </summary>
        /// <param name="prefix">The prefix</param>
        /// <returns>Semantic vocabulary for the given prefix</returns>
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

            Exception ex = new Exception(string.Format("Semantic vocabulary not found for prefix '{0}'", prefix));
            Log.Error(ex);
            // TODO should we throw the exception here or return the default vocabulary?
            throw ex;
        }

        /// <summary>
        /// Gets a XPM region by name.
        /// </summary>
        /// <param name="name">The region name</param>
        /// <returns>The XPM region matching the name for the given module</returns>
        public static XpmRegion GetXpmRegion(string name)
        {
            return GetXpmRegion(XpmRegions, name);
        }

        private static XpmRegion GetXpmRegion(IReadOnlyDictionary<string, XpmRegion> regions, string name)
        {
            if (regions.ContainsKey(name))
            {
                return regions[name];
            }
            else
            {
                Exception ex = new Exception(string.Format("XPM Region '{0}' does not exist.", name));
                //TODO - do we throw an error, or apply some defaults?
                Log.Error(ex);
                throw ex;   
            }        
        }

        /// <summary>
        /// Gets a semantic schema by id.
        /// </summary>
        /// <param name="id">The schema ID</param>
        /// <returns>The semantic schema matching the id for the given module</returns>
        public static SemanticSchema GetSchema(string id)
        {
            return GetSchema(SemanticMap, id);
        }

        private static SemanticSchema GetSchema(IReadOnlyDictionary<string, SemanticSchema> mapping, string id)
        {
            
            if (mapping.ContainsKey(id))
            {
                return mapping[id];
            }
            else
            {
                Exception ex = new Exception(string.Format("Semantic mappings for schema '{0}' do not exist.", id));
                //TODO - do we throw an error, or apply some defaults?
                Log.Error(ex);
                throw ex;
            }
        }

        private static void LoadMapping()
        {
            Load(AppDomain.CurrentDomain.BaseDirectory);
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

                _semanticMap = new Dictionary<string, SemanticSchema>();
                _semanticVocabularies = new List<SemanticVocabulary>();
                _xpmRegions = new Dictionary<string, XpmRegion>();

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
                                if (type.Equals("schemas"))
                                {
                                    foreach(var schema in GetSchemasFromFile(configPath))
                                    {
                                        _semanticMap.Add(schema.Id.ToString(), schema);
                                    }
                                }
                                else if (type.Equals("regions"))
                                {
                                    foreach (var region in GetRegionsFromFile(configPath))
                                    {
                                        _xpmRegions.Add(region.Region, region);
                                    }
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

        private static List<XpmRegion> GetRegionsFromFile(string file)
        {
            return new JavaScriptSerializer().Deserialize<List<XpmRegion>>(File.ReadAllText(file));
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
