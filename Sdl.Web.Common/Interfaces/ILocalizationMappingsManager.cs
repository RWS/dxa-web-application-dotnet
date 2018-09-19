using System.Collections.Generic;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Mapping;

namespace Sdl.Web.Common.Interfaces
{
    public interface ILocalizationMappingsManager
    {
        /// <summary>
        /// Reload all mappings for localization.
        /// </summary>
        void Reload();

        /// <summary>
        /// Manually set the semantic schemas instead of loading them automatically
        /// </summary>
        /// <param name="schemas">Schemas to use</param>
        /// <param name="vocab">Vocabularies to use</param>
        void SetSemanticSchemas(List<SemanticSchema> schemas, List<SemanticVocabulary> vocab);

        /// <summary>
        /// Add predefined schema.
        /// </summary>
        /// <param name="schema">Schema</param>
        void AddPredefinedSchema(SemanticSchema schema);

        /// <summary>
        /// Gets Semantic Schema for a given schema identifier.
        /// </summary>
        /// <param name="schemaId">The schema identifier.</param>
        /// <returns>The Semantic Schema configuration.</returns>
        SemanticSchema GetSemanticSchema(string schemaId);

        /// <summary>
        /// Gets the Semantic Vocabularies
        /// </summary>
        /// <returns></returns>
        IEnumerable<SemanticVocabulary> GetSemanticVocabularies();

        /// <summary>
        /// Gets a Semantic Vocabulary by a given prefix.
        /// </summary>
        /// <param name="prefix">The vocabulary prefix.</param>
        /// <returns>The Semantic Vocabulary.</returns>
        SemanticVocabulary GetSemanticVocabulary(string prefix);

        /// <summary>
        /// Gets XPM Region configuration for a given Region name.
        /// </summary>
        /// <param name="regionName">The Region name</param>
        /// <returns>The XPM Region configuration or <c>null</c> if no configuration is found.</returns>
        XpmRegion GetXpmRegionConfiguration(string regionName);

        /// <summary>
        /// Gets the include Page URLs for a given Page Type/Template.
        /// </summary>
        /// <param name="pageTypeIdentifier">The Page Type Identifier.</param>
        /// <returns>The URLs of Include Pages</returns>
        /// <remarks>
        /// The concept of Include Pages will be removed in a future version of DXA.
        /// As of DXA 1.1 Include Pages are represented as <see cref="Sdl.Web.Common.Models.PageModel.Regions"/>.
        /// Implementations should avoid using this method directly.
        /// </remarks>
        IEnumerable<string> GetIncludePageUrls(string pageTypeIdentifier);
    }
}
