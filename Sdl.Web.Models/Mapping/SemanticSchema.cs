using System.Collections.Generic;

namespace Sdl.Web.Mvc.Mapping
{
    /// <summary>
    /// Class for deserialized json schema.
    /// {"Id":80,"RootElement":"Article","Fields":[...],"Semantics":[...]}
    /// </summary>
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

        /// <summary>
        /// Initializes a new empty instance of the <see cref="SemanticSchema"/> class.
        /// </summary>
        public SemanticSchema() { }

        /// <summary>
        /// Find <see cref="SemanticSchemaField"/> with given semantic property.
        /// </summary>
        /// <param name="fieldSemantics">The semantic property to check against</param>
        /// <returns>Schema field or one of its embedded fields that match with the given semantic property, null if a match cannot be found</returns>
        public SemanticSchemaField FindFieldByProperty(FieldSemantics fieldSemantics)
        {
            foreach (var field in Fields)
            {
                var matchingField = field.FindFieldByProperty(fieldSemantics);
                if (matchingField != null)
                {
                    return matchingField;
                }
            }

            return null;
        }
    }
}
