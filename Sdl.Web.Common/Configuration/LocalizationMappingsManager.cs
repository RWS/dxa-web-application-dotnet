using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Mapping;

namespace Sdl.Web.Common.Configuration
{  
    /// <summary>
    /// Localization Mappings Manager
    /// </summary>
    public class LocalizationMappingsManager : ILocalizationMappingsManager
    {
        private readonly ILocalization _localization;

        // schemas for semantic mapping
        private SemanticSchema[] _semanticSchemas;
        private IDictionary<string, SemanticSchema> _semanticSchemaMap;
        private SemanticVocabulary[] _semanticVocabularies;
        private IDictionary<string, SemanticVocabulary> _semanticVocabularyMap;

        // regions
        private XpmRegion[] _xpmRegionConfiguration;
        private IDictionary<string, XpmRegion> _xpmRegionConfigurationMap;

        // include pages
        private IDictionary<string, string[]> _includePageUrls;


        private readonly object _loadLock = new object();


        public LocalizationMappingsManager(ILocalization localization)
        {
            _localization = localization;
        }

        /// <summary>
        /// Reload all mappings for localization.
        /// </summary>
        public virtual void Reload()
        {
            _semanticSchemas = null;
            _semanticVocabularies = null;
            _xpmRegionConfiguration = null;
        }

        /// <summary>
        /// Gets Semantic Schema for a given schema identifier.
        /// </summary>
        /// <param name="schemaId">The schema identifier.</param>
        /// <returns>The Semantic Schema configuration.</returns>
        public virtual SemanticSchema GetSemanticSchema(string schemaId)
        {
            // This method is called a lot, so intentionally no Tracer here.
            if (_semanticSchemas == null)
            {
                _localization.LoadStaticContentItem("mappings/schemas.json", ref _semanticSchemas);
                _semanticSchemaMap = _semanticSchemas.ToDictionary(ss => ss.Id.ToString(CultureInfo.InvariantCulture));
                foreach (SemanticSchema semanticSchema in _semanticSchemas)
                {
                    semanticSchema.Initialize(_localization);
                }
            }

            SemanticSchema result;
            if (!_semanticSchemaMap.TryGetValue(schemaId, out result))
            {
                throw new DxaException(
                    $"Semantic schema '{schemaId}' not defined in Localization [{this}]. {Constants.CheckSettingsUpToDate}"
                    );
            }

            return result;
        }

        /// <summary>
        /// Gets the Semantic Vocabularies
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<SemanticVocabulary> GetSemanticVocabularies()
        {
            // This method is called a lot, so intentionally no Tracer here.
            if (_semanticVocabularies == null)
            {
                _localization.LoadStaticContentItem("mappings/vocabularies.json", ref _semanticVocabularies);
            }
            return _semanticVocabularies;
        }

        /// <summary>
        /// Gets a Semantic Vocabulary by a given prefix.
        /// </summary>
        /// <param name="prefix">The vocabulary prefix.</param>
        /// <returns>The Semantic Vocabulary.</returns>
        public virtual SemanticVocabulary GetSemanticVocabulary(string prefix)
        {
            if (_semanticVocabularies == null)
            {
                _semanticVocabularyMap = GetSemanticVocabularies().ToDictionary(sv => sv.Prefix);
            }

            SemanticVocabulary result;
            if (!_semanticVocabularyMap.TryGetValue(prefix, out result))
            {
                throw new DxaException($"No vocabulary defined for prefix '{prefix}' in Localization [{this}]. {Constants.CheckSettingsUpToDate}");
            }

            return result;
        }

        /// <summary>
        /// Gets XPM Region configuration for a given Region name.
        /// </summary>
        /// <param name="regionName">The Region name</param>
        /// <returns>The XPM Region configuration or <c>null</c> if no configuration is found.</returns>
        public virtual XpmRegion GetXpmRegionConfiguration(string regionName)
        {
            // This method is called a lot, so intentionally no Tracer here.
            if (_xpmRegionConfiguration == null)
            {
                lock (_loadLock)
                {
                    if (_xpmRegionConfiguration == null)
                    {
                        _localization.LoadStaticContentItem("mappings/regions.json", ref _xpmRegionConfiguration);
                        _xpmRegionConfigurationMap = _xpmRegionConfiguration.ToDictionary(xpmRegion => xpmRegion.Region);
                    }
                }
            }

            XpmRegion result;
            if (!_xpmRegionConfigurationMap.TryGetValue(regionName, out result))
            {
                Log.Warn("XPM Region '{0}' is not defined in Localization [{1}].", regionName, this);
            }

            return result;
        }

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
        public virtual IEnumerable<string> GetIncludePageUrls(string pageTypeIdentifier)
        {
            using (new Tracer(pageTypeIdentifier, this))
            {
                if (_includePageUrls == null)
                {
                    _localization.LoadStaticContentItem("mappings/includes.json", ref _includePageUrls);
                }

                string[] result;
                if (!_includePageUrls.TryGetValue(pageTypeIdentifier, out result))
                {
                    throw new DxaException(
                        $"Localization [{this}] does not contain includes for Page Type '{pageTypeIdentifier}'. {Constants.CheckSettingsUpToDate}"
                        );
                }

                return result;
            }
        }
    }
}
