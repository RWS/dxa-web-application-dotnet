using System.Linq;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Common.Mapping
{
    /// <summary>
    /// Starting point for Semantic Mapping functionality.
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
        /// Gets a qualified (semantic) type name consisting of vocabulary ID and (local) type name.
        /// </summary>
        /// <param name="typeName">The (local) type name.</param>
        /// <param name="vocab">The vocabulary ID or <c>null</c> for the default/core vocabulary.</param>
        /// <returns>The qualified type name.</returns>
        public static string GetQualifiedTypeName(string typeName, string vocab = null) => $"{vocab ?? DefaultVocabulary}:{typeName}";

        /// <summary>
        /// Gets a qualified (semantic) type name consisting of vocabulary ID and (local) type name.
        /// </summary>
        /// <param name="typeName">The (local) type name.</param>
        /// <param name="prefix">The vocabulary prefix.</param>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The qualified type name.</returns>
        public static string GetQualifiedTypeName(string typeName, string prefix, Localization localization) => GetQualifiedTypeName(typeName, GetVocabulary(prefix, localization));

        /// <summary>
        /// Gets semantic vocabulary by prefix.
        /// </summary>
        /// <param name="prefix">The prefix</param>
        /// <param name="loc">The localization</param>
        /// <returns>Semantic vocabulary for the given prefix</returns>
        public static string GetVocabulary(string prefix, Localization loc) => loc.GetSemanticVocabulary(prefix).Vocab;

        /// <summary>
        /// Gets prefix for semantic vocabulary.
        /// </summary>
        /// <param name="vocab">Vocabulary name</param>
        /// <param name="loc">The localization</param>
        /// <returns>Prefix for this semantic vocabulary</returns>
        public static string GetPrefix(string vocab, Localization loc)
        {
            SemanticVocabulary semanticVocabulary = loc.GetSemanticVocabularies().FirstOrDefault(sv => sv.Vocab == vocab);
            if (semanticVocabulary == null)
            {
                throw new DxaException(
                    $"No vocabulary defined for '{vocab}' in Localization [{loc}]. {Constants.CheckSettingsUpToDate}"
                    );
            }
            return semanticVocabulary.Prefix;
        }

        /// <summary>
        /// Gets a semantic schema by id.
        /// </summary>
        /// <param name="id">The schema ID</param>
        /// <param name="loc">The localization</param>
        /// <returns>The semantic schema matching the id for the given module</returns>
        public static SemanticSchema GetSchema(string id, Localization loc) => loc.GetSemanticSchema(id);
    }
}
