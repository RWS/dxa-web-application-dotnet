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

        private static Dictionary<string, Dictionary<string, SemanticSchema>> _semanticMap = new Dictionary<string,Dictionary<string,SemanticSchema>>();
        private static Dictionary<string, List<SemanticVocabulary>> _semanticVocabularies = new Dictionary<string,List<SemanticVocabulary>>();
        private static Dictionary<string, Dictionary<string, List<string>>> _includes = new Dictionary<string,Dictionary<string,List<string>>>();

        private static Dictionary<string, DateTime> _mapLastRefreshes = new Dictionary<string, DateTime>();
        private static Dictionary<string, DateTime> _vocabLastRefreshes = new Dictionary<string, DateTime>();
        private static Dictionary<string, DateTime> _includeLastRefreshes = new Dictionary<string, DateTime>();

        private static readonly object MappingLock = new object();

        /// <summary>
        /// Gets semantic vocabulary by prefix.
        /// </summary>
        /// <param name="prefix">The prefix</param>
        /// <returns>Semantic vocabulary for the given prefix</returns>
        public static string GetVocabulary(string prefix, Localization loc)
        {
            var key = loc.LocalizationId;
            if (!_semanticVocabularies.ContainsKey(loc.LocalizationId) || (_vocabLastRefreshes.ContainsKey(key) && _vocabLastRefreshes[key] < loc.LastSettingsRefresh))
            {
                LoadVocabulariesForLocalization(loc);
            }
            if (_semanticVocabularies.ContainsKey(loc.LocalizationId))
            {
                var vocabs = _semanticVocabularies[loc.LocalizationId];
                return GetVocabulary(vocabs, prefix);
            }
            Log.Error("Localization {0} does not contain prefix {1}. Check that the Publish Settings page is published and the application cache is up to date.", loc.LocalizationId, prefix);
            return null;
        }

        /// <summary>
        /// Gets prefix for semantic vocabulary.
        /// </summary>
        /// <param name="vocab">Vocabulary name</param>
        /// <returns>Prefix for this semantic vocabulary</returns>
        public static string GetPrefix(string vocab, Localization loc)
        {
            var key = loc.LocalizationId;
            if (!_semanticVocabularies.ContainsKey(key) || (_vocabLastRefreshes.ContainsKey(key) && _vocabLastRefreshes[key] < loc.LastSettingsRefresh))
            {
                LoadVocabulariesForLocalization(loc);
            }
            if (_semanticVocabularies.ContainsKey(key))
            {
                var vocabs = _semanticVocabularies[key];
                return GetPrefix(vocabs, vocab);
            }
            Log.Error("Localization {0} does not contain vocabulary {1}. Check that the Publish Settings page is published and the application cache is up to date.", loc.LocalizationId, vocab);
            return null;
        }

        /// <summary>
        /// Gets an include by name
        /// </summary>
        /// <param name="pageTypeIdentifier">The include name</param>
        /// <returns>The include matching the name </returns>
        public static List<string> GetIncludes(string pageTypeIdentifier, Localization loc)
        {
            var key = loc.LocalizationId;
            if (!_includes.ContainsKey(key) || (_includeLastRefreshes.ContainsKey(key) && _includeLastRefreshes[key] < loc.LastSettingsRefresh))
            {
                LoadIncludesForLocalization(loc);
            }
            if (_includes.ContainsKey(key))
            {
                var includes = _includes[key];
                if (includes.ContainsKey(pageTypeIdentifier))
                {
                    return includes[pageTypeIdentifier];
                }
            }
            Log.Error("Localization {0} does not contain include {1}. Check that the Publish Settings page is published and the application cache is up to date.", loc.LocalizationId, pageTypeIdentifier);
            return null;
        }

        /// <summary>
        /// Gets a semantic schema by id.
        /// </summary>
        /// <param name="id">The schema ID</param>
        /// <returns>The semantic schema matching the id for the given module</returns>
        public static SemanticSchema GetSchema(string id, Localization loc)
        {
            var key = loc.LocalizationId;
            if (!_semanticMap.ContainsKey(key) || (_mapLastRefreshes.ContainsKey(key) && _mapLastRefreshes[key] < loc.LastSettingsRefresh))
            {
                LoadSemanticMapForLocalization(loc);
            }
            if (_semanticMap.ContainsKey(key))
            {
                var map = _semanticMap[key];
                return GetSchema(map, id);
            }
            Log.Error("Localization {0} does not contain semantic schema map {1}. Check that the Publish Settings page is published and the application cache is up to date.", loc.LocalizationId, id);
            return null;
        }

        private static void LoadVocabulariesForLocalization(Localization loc)
        {
            if (_vocabLastRefreshes.ContainsKey(loc.LocalizationId))
            {
                _vocabLastRefreshes[loc.LocalizationId] = DateTime.Now;
            }
            else
            {
                _vocabLastRefreshes.Add(loc.LocalizationId, DateTime.Now);
            }
            var key = loc.LocalizationId;
            var url = Path.Combine(loc.Path.ToCombinePath(), SiteConfiguration.SystemFolder, @"mappings\vocabularies.json").Replace("\\", "/");
            var jsonData = SiteConfiguration.StaticFileManager.Serialize(url, loc, true);
            if (jsonData != null)
            {
                var vocabs = GetVocabulariesFromFile(jsonData);
                if (_semanticVocabularies.ContainsKey(key))
                {
                    _semanticVocabularies[key] = vocabs;
                }
                else
                {
                    _semanticVocabularies.Add(key, vocabs);
                }
            }
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

        
        private static void LoadIncludesForLocalization(Localization loc)
        {
            if (_includeLastRefreshes.ContainsKey(loc.LocalizationId))
            {
                _includeLastRefreshes[loc.LocalizationId] = DateTime.Now;
            }
            else
            {
                _includeLastRefreshes.Add(loc.LocalizationId, DateTime.Now);
            }
            var key = loc.LocalizationId;
            var url = Path.Combine(loc.Path.ToCombinePath(), SiteConfiguration.SystemFolder, @"mappings\includes.json").Replace("\\", "/");
            var jsonData = SiteConfiguration.StaticFileManager.Serialize(url, loc, true);
            if (jsonData!=null)
            {
                var includes = GetIncludesFromFile(jsonData);
                if (_semanticVocabularies.ContainsKey(key))
                {
                    _includes[key] = includes;
                }
                else
                {
                    _includes.Add(key, includes);
                }
            }
        }

        private static void LoadSemanticMapForLocalization(Localization loc)
        {
            if (_includeLastRefreshes.ContainsKey(loc.LocalizationId))
            {
                _includeLastRefreshes[loc.LocalizationId] = DateTime.Now;
            }
            else
            {
                _includeLastRefreshes.Add(loc.LocalizationId, DateTime.Now);
            }
            var key = loc.LocalizationId;
            var url = Path.Combine(loc.Path.ToCombinePath(), SiteConfiguration.SystemFolder, @"mappings\schemas.json").Replace("\\", "/");
            var jsonData = SiteConfiguration.StaticFileManager.Serialize(url, loc, true);
            if (jsonData != null)
            {
                var schemas = GetSchemasFromFile(jsonData);
                var map = new Dictionary<string, SemanticSchema>();
                foreach (var schema in schemas)
                {
                    schema.Localization = loc;
                    map.Add(schema.Id.ToString(CultureInfo.InvariantCulture), schema);
                }
                if (_semanticVocabularies.ContainsKey(key))
                {
                    _semanticMap[key] = map;
                }
                else
                {
                    _semanticMap.Add(key, map);
                }
            }
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

        private static Dictionary<string, List<string>> GetIncludesFromFile(string jsonData)
        {
            return new JavaScriptSerializer().Deserialize<Dictionary<string,List<string>>>(jsonData);
        }

        private static IEnumerable<SemanticSchema> GetSchemasFromFile(string jsonData)
        {
            return new JavaScriptSerializer().Deserialize<List<SemanticSchema>>(jsonData);
        }

        private static List<SemanticVocabulary> GetVocabulariesFromFile(string jsonData)
        {
            return new JavaScriptSerializer().Deserialize<List<SemanticVocabulary>>(jsonData);
        }
    }
}
