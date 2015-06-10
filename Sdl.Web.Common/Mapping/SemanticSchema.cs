using System.Collections.Generic;
using System.Linq;
using Sdl.Web.Common.Configuration;

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
    }
}
