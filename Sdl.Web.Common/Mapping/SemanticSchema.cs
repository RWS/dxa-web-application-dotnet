using System;
using System.Collections.Generic;
using System.Linq;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Mapping
{
    /// <summary>
    /// Represents a Semantic Schema
    /// </summary>
    /// <remarks>
    /// Deserialized from JSON in schemas.json.
    /// </remarks>
    public class SemanticSchema
    {
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

        public ILocalization Localization { get; set; }

        /// <summary>
        /// Initializes an existing instance.
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        public void Initialize(ILocalization localization)
        {
            Localization = localization;
            foreach (SchemaSemantics semantics in Semantics)
            {
                semantics.Initialize(localization);
            }
            foreach (SemanticSchemaField field in Fields)
            {
                field.Initialize(localization);
            }
        }

        /// <summary>
        /// Get the Schema's semantic entity/types names grouped per semantic vocabulary. 
        /// </summary>
        /// <remarks>
        /// Using <see cref="ILookup{TKey,TElement}"/> rather than a <see cref="Dictionary{TKey,TValue}"/> because it will allow for duplicate keys.
        /// </remarks>
        /// <returns>The Schema's semantic entity/types names grouped per semantic vocabulary</returns>
        public ILookup<string, string> GetEntityNames() => Semantics.ToLookup(ss => ss.Vocab, ss => ss.Entity);

        /// <summary>
        /// Find <see cref="SemanticSchemaField"/> with given semantics.
        /// </summary>
        /// <param name="fieldSemantics">The semantics to check against</param>
        /// <returns>Schema field or one of its embedded fields that match with the given semantics, null if a match cannot be found</returns>
        public SemanticSchemaField FindFieldBySemantics(FieldSemantics fieldSemantics)
        {
            // Perform a breadth-first lookup: first see if any of the top-level fields match.
            SemanticSchemaField matchingTopLevelField = Fields.FirstOrDefault(ssf => ssf.HasSemantics(fieldSemantics));
            if (matchingTopLevelField != null)
            {
                return matchingTopLevelField;
            }

            // If none of the top-level fields match: let each top-level field do a breadth-first lookup of its embedded fields (recursive).
            return Fields.Select(ssf => ssf.FindFieldBySemantics(fieldSemantics)).FirstOrDefault(matchingField => matchingField != null);
        }

        /// <summary>
        /// Gets semantic type names (qualified with Vocabulary ID) for the Schema.
        /// </summary>
        /// <returns>The semantic type names.</returns>
        public string[] GetSemanticTypeNames() => Semantics.Select(ss => ss.ToString()).ToArray();

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
                errorMessage =
                    $"No semantic mapping found between Schema {Id} ({String.Join(", ", semanticTypeNames)}) and model type '{baseModelType.FullName}'";
            }
            else
            {
                errorMessage =
                    $"Ambiguous semantic mappings found between Schema {Id} ({String.Join(", ", semanticTypeNames)}) and model type '{String.Join(", ", foundAmbiguousMappings.Select(t => t.FullName))}'. Found types: {baseModelType.FullName}";
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

        public bool HasSemanticType(SemanticType semanticType)
        {
            SchemaSemantics semantics = new SchemaSemantics(semanticType.Vocab, semanticType.EntityName, null);
            return Semantics.Any(s => s.Equals(semantics));
        }

        /// <summary>
        /// Provides a string representation of the object.
        /// </summary>
        /// <returns>A string representation containing the Schema ID and Root Element name</returns>
        public override string ToString() => $"{GetType().Name} {Id} ({RootElement})";
    }
}
