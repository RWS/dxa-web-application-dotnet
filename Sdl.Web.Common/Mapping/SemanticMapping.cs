using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.Helpers;
using System.Web.Script.Serialization;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Logging;

namespace Sdl.Web.Common.Mapping
{
    /// <summary>
    /// General Semantic Mapping Class which reads schema mapping from json files on disk
    /// </summary>
    public static class SemanticMapping
    {
        /// <summary>
        /// Default semantic vocabulary prefix.
        /// </summary>
        public const string DefaultPrefix = "tri";

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

        private static Dictionary<string, SemanticSchema> _semanticMap;
        private static List<SemanticVocabulary> _semanticVocabularies;
        private static Dictionary<string, List<string>> _includes;
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

            Log.Warn("Prefix not found for semantic vocabulary '{0}'", vocab);
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
        /// <param name="pageTypeIdentifier">The region name</param>
        /// <returns>The XPM region matching the name for the given module</returns>
        public static List<string> GetIncludes(string pageTypeIdentifier)
        {
            if (_includes == null)
            {
                LoadMapping();
            }
            if (_includes.ContainsKey(pageTypeIdentifier))
            {
                return _includes[pageTypeIdentifier];
            }
            return null;
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
            Log.Error("Semantic mappings for schema '{0}' do not exist.", id);
            return null;
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
                var semanticMap = new Dictionary<string, SemanticSchema>();
                var semanticVocabularies = new List<SemanticVocabulary>();
                var includes = new Dictionary<string, List<string>>();
                Log.Debug("Loading semantic mappings for default localization");
                var path = Path.Combine(new[] { applicationRoot, SiteConfiguration.StaticsFolder, SiteConfiguration.DefaultLocalization, SiteConfiguration.SystemFolder, @"mappings\_all.json" });
                if (File.Exists(path))
                {
                    // the _all.json file contains a reference to all other configuration files
                    Log.Debug("Loading semantic mapping bootstrap file : '{0}'", path);
                    var bootstrapJson = Json.Decode(File.ReadAllText(path));
                    foreach (string file in bootstrapJson.files)
                    {
                        var type = file.Substring(file.LastIndexOf("/", StringComparison.Ordinal) + 1);
                        type = type.Substring(0, type.LastIndexOf(".", StringComparison.Ordinal)).ToLower();
                        var configPath = Path.Combine(new[] { applicationRoot, SiteConfiguration.StaticsFolder, file.ToCombinePath() });
                        if (File.Exists(configPath))
                        {
                            Log.Debug("Loading mapping from file: {0}", configPath);
                            if (type.Equals("vocabularies"))
                            {
                                semanticVocabularies = GetVocabulariesFromFile(configPath);
                            }
                            else
                            {
                                if (type.Equals("schemas"))
                                {
                                    foreach(var schema in GetSchemasFromFile(configPath))
                                    {
                                        semanticMap.Add(schema.Id.ToString(CultureInfo.InvariantCulture), schema);
                                    }
                                }
                                else if (type.Equals("includes"))
                                {
                                    includes = GetIncludesFromFile(configPath);
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
                SemanticMapping._semanticVocabularies = semanticVocabularies;
                SemanticMapping._semanticMap = semanticMap;
                SemanticMapping._includes = includes;
            }
        }

        private static Dictionary<string, List<string>> GetIncludesFromFile(string file)
        {
            return new JavaScriptSerializer().Deserialize<Dictionary<string,List<string>>>(File.ReadAllText(file));
        }

        private static IEnumerable<SemanticSchema> GetSchemasFromFile(string file)
        {
            return new JavaScriptSerializer().Deserialize<List<SemanticSchema>>(File.ReadAllText(file));
        }

        private static List<SemanticVocabulary> GetVocabulariesFromFile(string file)
        {
            return new JavaScriptSerializer().Deserialize<List<SemanticVocabulary>>(File.ReadAllText(file));
        }
    }
}
