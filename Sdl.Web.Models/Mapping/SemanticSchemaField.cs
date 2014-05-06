using System.Collections.Generic;

namespace Sdl.Web.Mvc.Mapping
{
    /// <summary>
    /// Class for deserialized json schema field.
    /// {"Name":"headline","IsMultiValue":false,"Semantics":[...],"Fields":[...]}
    /// </summary>
    public class SemanticSchemaField
    {
        /// <summary>
        /// XML field name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Is field multivalued?
        /// </summary>
        public bool IsMultiValue { get; set; }

        /// <summary>
        /// Field semantics.
        /// </summary>
        public List<FieldSemantics> Semantics { get; set; }

        /// <summary>
        /// Embedded fields.
        /// </summary>
        public List<SemanticSchemaField> Fields { get; set; }

        /// <summary>
        /// Initializes a new empty instance of the <see cref="SemanticSchemaField"/> class.
        /// </summary>
        public SemanticSchemaField() { }

        /// <summary>
        /// Check if current field contains given semantic property.
        /// </summary>
        /// <param name="fieldSemantics">The semantic property to check against</param>
        /// <returns>True if this field contains given semantics, false otherwise</returns>
        public bool ContainsProperty(FieldSemantics fieldSemantics)
        {
            foreach (var property in Semantics)
            {
                if (property.Equals(fieldSemantics))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Find <see cref="SemanticSchemaField"/> with given semantic property.
        /// </summary>
        /// <param name="fieldSemantics">The semantic property to check against</param>
        /// <returns>This field or one of its embedded fields that match with the given semantics, null if a match cannot be found</returns>
        public SemanticSchemaField FindFieldByProperty(FieldSemantics fieldSemantics)
        {
            if (ContainsProperty(fieldSemantics))
            {
                return this;
            }

            foreach (var embeddedField in Fields)
            {
                SemanticSchemaField field = embeddedField.FindFieldByProperty(fieldSemantics);
                if (field != null)
                {
                    return field;
                }
            }
            return null;
        }
    }
}
