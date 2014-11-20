using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.Script.Serialization;

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

        private const string MapSettingsType = "map";
        private static readonly Dictionary<string, Dictionary<string, SemanticSchema>> SemanticMap = new Dictionary<string, Dictionary<string, SemanticSchema>>();
        private const string VocabSettingsType = "vocab";
        private static readonly Dictionary<string, List<SemanticVocabulary>> SemanticVocabularies = new Dictionary<string, List<SemanticVocabulary>>();
        private const string IncludeSettingsType = "include";
        private static readonly Dictionary<string, Dictionary<string, List<string>>> Includes = new Dictionary<string, Dictionary<string, List<string>>>();

        /// <summary>
        /// Gets semantic vocabulary by prefix.
        /// </summary>
        /// <param name="prefix">The prefix</param>
        /// <param name="loc">The localization</param>
        /// <returns>Semantic vocabulary for the given prefix</returns>
        public static string GetVocabulary(string prefix, Localization loc)
        {
            var key = loc.LocalizationId;
            if (!SemanticVocabularies.ContainsKey(key) || SiteConfiguration.CheckSettingsNeedRefresh(VocabSettingsType, loc.LocalizationId))
            {
                LoadVocabulariesForLocalization(loc);
            }
            if (SemanticVocabularies.ContainsKey(key))
            {
                var vocabs = SemanticVocabularies[key];
                return GetVocabulary(vocabs, prefix);
            }
            Log.Error("Localization {0} does not contain prefix {1}. Check that the Publish Settings page is published and the application cache is up to date.", loc.LocalizationId, prefix);
            return null;
        }

        /// <summary>
        /// Gets prefix for semantic vocabulary.
        /// </summary>
        /// <param name="vocab">Vocabulary name</param>
        /// <param name="loc">The localization</param>
        /// <returns>Prefix for this semantic vocabulary</returns>
        public static string GetPrefix(string vocab, Localization loc)
        {
            var key = loc.LocalizationId;
            if (!SemanticVocabularies.ContainsKey(key) || SiteConfiguration.CheckSettingsNeedRefresh(VocabSettingsType, loc.LocalizationId))
            {
                LoadVocabulariesForLocalization(loc);
            }
            if (SemanticVocabularies.ContainsKey(key))
            {
                var vocabs = SemanticVocabularies[key];
                return GetPrefix(vocabs, vocab);
            }
            Log.Error("Localization {0} does not contain vocabulary {1}. Check that the Publish Settings page is published and the application cache is up to date.", loc.LocalizationId, vocab);
            return null;
        }

        /// <summary>
        /// Gets an include by name
        /// </summary>
        /// <param name="pageTypeIdentifier">The include name</param>
        /// <param name="loc">The localization</param>
        /// <returns>The include matching the name </returns>
        public static List<string> GetIncludes(string pageTypeIdentifier, Localization loc)
        {
            var key = loc.LocalizationId;
            if (!Includes.ContainsKey(key) || SiteConfiguration.CheckSettingsNeedRefresh(IncludeSettingsType, loc.LocalizationId))
            {
                LoadIncludesForLocalization(loc);
            }
            if (Includes.ContainsKey(key))
            {
                var includes = Includes[key];
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
        /// <param name="loc">The localization</param>
        /// <returns>The semantic schema matching the id for the given module</returns>
        public static SemanticSchema GetSchema(string id, Localization loc)
        {
            var key = loc.LocalizationId;
            if (!SemanticMap.ContainsKey(key) || SiteConfiguration.CheckSettingsNeedRefresh(MapSettingsType, loc.LocalizationId))
            {
                LoadSemanticMapForLocalization(loc);
            }
            if (SemanticMap.ContainsKey(key))
            {
                var map = SemanticMap[key];
                return GetSchema(map, id);
            }
            Log.Error("Localization {0} does not contain semantic schema map {1}. Check that the Publish Settings page is published and the application cache is up to date.", loc.LocalizationId, id);
            return null;
        }

        private static void LoadVocabulariesForLocalization(Localization loc)
        {
            var key = loc.LocalizationId;
            var url = Path.Combine(loc.Path.ToCombinePath(true), SiteConfiguration.SystemFolder, @"mappings\vocabularies.json").Replace("\\", "/");
            var jsonData = SiteConfiguration.StaticFileManager.Serialize(url, loc, true);
            if (jsonData != null)
            {
                var vocabs = GetVocabulariesFromFile(jsonData);
                SiteConfiguration.ThreadSafeSettingsUpdate(VocabSettingsType, SemanticVocabularies, key, vocabs);
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
            var key = loc.LocalizationId;
            var url = Path.Combine(loc.Path.ToCombinePath(true), SiteConfiguration.SystemFolder, @"mappings\includes.json").Replace("\\", "/");
            var jsonData = SiteConfiguration.StaticFileManager.Serialize(url, loc, true);
            if (jsonData!=null)
            {
                var includes = GetIncludesFromFile(jsonData);
                SiteConfiguration.ThreadSafeSettingsUpdate(IncludeSettingsType, Includes, key, includes);
            }
        }

        private static void LoadSemanticMapForLocalization(Localization loc)
        {
            var key = loc.LocalizationId;
            var url = Path.Combine(loc.Path.ToCombinePath(true), SiteConfiguration.SystemFolder, @"mappings\schemas.json").Replace("\\", "/");
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
                SiteConfiguration.ThreadSafeSettingsUpdate(MapSettingsType, SemanticMap, key, map);
            }
        }

        private static SemanticSchema GetSchema(IReadOnlyDictionary<string, SemanticSchema> mapping, string id)
        {            
            if (mapping.ContainsKey(id))
            {
                return mapping[id];
            }
            Log.Error("Semantic mappings for Schema '{0}' do not exist.", id);
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
