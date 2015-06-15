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

        private const string _mapSettingsType = "map";
        private const string _vocabSettingsType = "vocab";

        private static readonly Dictionary<string, Dictionary<string, SemanticSchema>> _semanticMap = new Dictionary<string, Dictionary<string, SemanticSchema>>();
        private static readonly Dictionary<string, List<SemanticVocabulary>> _semanticVocabularies = new Dictionary<string, List<SemanticVocabulary>>();

        /// <summary>
        /// Gets a qualified (semantic) type name consisting of vocabulary ID and (local) type name.
        /// </summary>
        /// <param name="typeName">The (local) type name.</param>
        /// <param name="vocab">The vocabulary ID or <c>null</c> for the default/core vocabulary.</param>
        /// <returns>The qualified type name.</returns>
        public static string GetQualifiedTypeName(string typeName, string vocab = null)
        {
            return string.Format("{0}:{1}", vocab ?? DefaultVocabulary, typeName);
        }

        /// <summary>
        /// Gets a qualified (semantic) type name consisting of vocabulary ID and (local) type name.
        /// </summary>
        /// <param name="typeName">The (local) type name.</param>
        /// <param name="prefix">The vocabulary prefix.</param>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The qualified type name.</returns>
        public static string GetQualifiedTypeName(string typeName, string prefix, Localization localization)
        {
            return GetQualifiedTypeName(typeName, GetVocabulary(prefix, localization));
        }

        /// <summary>
        /// Gets semantic vocabulary by prefix.
        /// </summary>
        /// <param name="prefix">The prefix</param>
        /// <param name="loc">The localization</param>
        /// <returns>Semantic vocabulary for the given prefix</returns>
        public static string GetVocabulary(string prefix, Localization loc)
        {
            string key = loc.LocalizationId;
            if (!_semanticVocabularies.ContainsKey(key) || SiteConfiguration.CheckSettingsNeedRefresh(_vocabSettingsType, loc.LocalizationId))
            {
                LoadVocabulariesForLocalization(loc);
            }
            if (_semanticVocabularies.ContainsKey(key))
            {
                List<SemanticVocabulary> vocabs = _semanticVocabularies[key];
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
            string key = loc.LocalizationId;
            if (!_semanticVocabularies.ContainsKey(key) || SiteConfiguration.CheckSettingsNeedRefresh(_vocabSettingsType, loc.LocalizationId))
            {
                LoadVocabulariesForLocalization(loc);
            }
            if (_semanticVocabularies.ContainsKey(key))
            {
                List<SemanticVocabulary> vocabs = _semanticVocabularies[key];
                return GetPrefix(vocabs, vocab);
            }
            Log.Error("Localization {0} does not contain vocabulary {1}. Check that the Publish Settings page is published and the application cache is up to date.", loc.LocalizationId, vocab);
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
            string key = loc.LocalizationId;
            if (!_semanticMap.ContainsKey(key) || SiteConfiguration.CheckSettingsNeedRefresh(_mapSettingsType, loc.LocalizationId))
            {
                LoadSemanticMapForLocalization(loc);
            }

            try
            {
                return _semanticMap[key][id];
            }
            catch (Exception)
            {
                throw new DxaException(
                    string.Format("Semantic schema {0} not defined in Localization [{1}]. Check that the Publish Settings page is published and the application cache is up to date.", 
                    id, loc)
                    );
            }
        }

        private static void LoadVocabulariesForLocalization(Localization loc)
        {
            string key = loc.LocalizationId;
            string url = Path.Combine(loc.Path.ToCombinePath(true), SiteConfiguration.SystemFolder, @"mappings\vocabularies.json").Replace("\\", "/");
            string jsonData = SiteConfiguration.ContentProvider.GetStaticContentItem(url, loc).GetText();
            if (jsonData != null)
            {
                List<SemanticVocabulary> vocabs = GetVocabulariesFromFile(jsonData);
                SiteConfiguration.ThreadSafeSettingsUpdate(_vocabSettingsType, _semanticVocabularies, key, vocabs);
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

        
        private static void LoadSemanticMapForLocalization(Localization loc)
        {
            string key = loc.LocalizationId;
            string url = Path.Combine(loc.Path.ToCombinePath(true), SiteConfiguration.SystemFolder, @"mappings\schemas.json").Replace("\\", "/");
            string jsonData = SiteConfiguration.ContentProvider.GetStaticContentItem(url, loc).GetText();;
            if (jsonData != null)
            {
                IEnumerable<SemanticSchema> schemas = GetSchemasFromFile(jsonData);
                Dictionary<string, SemanticSchema> map = new Dictionary<string, SemanticSchema>();
                foreach (SemanticSchema schema in schemas)
                {
                    schema.Localization = loc;
                    map.Add(schema.Id.ToString(CultureInfo.InvariantCulture), schema);
                }
                SiteConfiguration.ThreadSafeSettingsUpdate(_mapSettingsType, _semanticMap, key, map);
            }
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
