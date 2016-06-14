using System;
using System.Collections.Generic;
using System.Linq;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Mapping
{
    /// <summary>
    /// Class for deserialized json schema.
    /// {"Id":80,"RootElement":"Article","Fields":[...],"Semantics":[...]}
    /// </summary>
    public class SemanticSchema
    {
        private string[] _semanticTypeNames; 

        /// <summary>
        /// Schema (item) ID.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Schema root element name.
        /// </summary>
        public string RootElement { get; set; }

        /// <summary>
        /// Schema fields.
        /// </summary>
        public List<SemanticSchemaField> Fields { get; set; }

        /// <summary>
        /// Schema semantics.
        /// </summary>
        public List<SchemaSemantics> Semantics { get; set; }


        public Localization Localization { get; set; }

        /// <summary>
        /// Initializes a new empty instance of the <see cref="SemanticSchema"/> class.
        /// </summary>
        public SemanticSchema() { }

        /// <summary>
        /// Get a list with schema entity names for its vocabularies. 
        /// Using vocabulary name rather than prefix from json, as the prefixes can be different in the view. 
        /// </summary>
        /// <remarks>
        /// Using <see cref="ILookup{TKey,TElement}"/> rather than a <see cref="Dictionary{TKey,TValue}"/> because it will allow for duplicate keys.
        /// Duplicate keys make no sense, but we might have them, so this prevents runtime exceptions.
        /// </remarks>
        /// <returns>List with entity names indexed by vocabulary</returns>
        public ILookup<string, string> GetEntityNames()
        {
            List<KeyValuePair<string, string>> entityNames = new List<KeyValuePair<string, string>>();
            foreach (SchemaSemantics schemaSemantics in Semantics)
            {
                string vocab = SemanticMapping.GetVocabulary(schemaSemantics.Prefix, Localization);
                entityNames.Add(new KeyValuePair<string, string>(vocab, schemaSemantics.Entity));
            }

            return entityNames.ToLookup(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// Find <see cref="SemanticSchemaField"/> with given semantics.
        /// </summary>
        /// <param name="fieldSemantics">The semantics to check against</param>
        /// <returns>Schema field or one of its embedded fields that match with the given semantics, null if a match cannot be found</returns>
        public SemanticSchemaField FindFieldBySemantics(FieldSemantics fieldSemantics)
        {
            foreach (SemanticSchemaField field in Fields)
            {
                SemanticSchemaField matchingField = field.FindFieldBySemantics(fieldSemantics);
                if (matchingField != null)
                {
                    return matchingField;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets semantic type names (qualified with Vocabulary ID) for the Schema.
        /// </summary>
        /// <returns>The semantic type names.</returns>
        public string[] GetSemanticTypeNames()
        {
            if (_semanticTypeNames == null)
            {
                _semanticTypeNames = Semantics.Select(s => SemanticMapping.GetQualifiedTypeName(s.Entity, s.Prefix, Localization)).ToArray();
            }
            return _semanticTypeNames;
        }

        /// <summary>
        /// Determine a Model Type based on semantic mappings (and a given base model type).
        /// </summary>
        /// <param name="baseModelType">The base type as obtained from the View Model.</param>
        /// <returns>The given base Model Type or a subclass if a more specific class can be resolved via semantic mapping.</returns>
        /// <remarks>
        /// This method makes it possible (for example) to let the <see cref="Teaser.Media"/> property get an instance of <see cref="Image"/> 
        /// rather than just <see cref="MediaItem"/> (the type of the View Model property).
        /// </remarks>
        public Type GetModelTypeFromSemanticMapping(Type baseModelType)
        {
            Type[] foundAmbiguousMappings = null;
            string[] semanticTypeNames = GetSemanticTypeNames();
            foreach (string semanticTypeName in semanticTypeNames)
            {
                IEnumerable<Type> mappedModelTypes = ModelTypeRegistry.GetMappedModelTypes(semanticTypeName);
                if (mappedModelTypes == null)
                {
                    continue;
                }

                Type[] matchingModelTypes = mappedModelTypes.Where(t => baseModelType.IsAssignableFrom(t)).ToArray();
                if (matchingModelTypes.Length == 1)
                {
                    // Exactly one matching model type; return it.
                    return matchingModelTypes[0];
                }
                
                if (matchingModelTypes.Length > 1)
                {
                    // Multiple candidate models types found. Continue scanning; maybe we'll find a unique one for another semantic type.
                    foundAmbiguousMappings = matchingModelTypes;
                }
            }

            string errorMessage;
            if (foundAmbiguousMappings == null)
            {
                errorMessage = string.Format("No semantic mapping found between Schema {0} ({1}) and model type '{2}'",
                    Id, String.Join(", ", semanticTypeNames), baseModelType.FullName);
            }
            else
            {
                errorMessage = string.Format("Ambiguous semantic mappings found between Schema {0} ({1}) and model type '{2}'. Found types: {3}",
                    Id, String.Join(", ", semanticTypeNames), String.Join(", ", foundAmbiguousMappings.Select(t => t.FullName)), baseModelType.FullName);
            }

            if (baseModelType.IsAbstract)
            {
                // Base model type is abstract and we didn't find an (unambigous) concrete subtype to instantiate.
                throw new DxaException(errorMessage);
            }

            // Base model type is concrete, so we can fall back to instantiating that type.
            if (foundAmbiguousMappings == null)
            {
                Log.Debug("{0}. Sticking with model type.", errorMessage);
            }
            else
            {
                Log.Warn("{0}. Sticking with model type.", errorMessage);
            }

            return baseModelType;
        }
    }
}
