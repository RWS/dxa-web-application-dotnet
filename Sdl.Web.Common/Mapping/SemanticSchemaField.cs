using System.Collections.Generic;

namespace Sdl.Web.Common.Mapping
{
    /// <summary>
    /// Class for deserialized json schema field.
    /// {"Name":"headline","Path":"/Article/headline","IsMultiValue":false,"Semantics":[...],"Fields":[...]}
    /// </summary>
    public class SemanticSchemaField
    {
        /// <summary>
        /// XML field name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// XML field path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Is field a metadata field?
        /// </summary>
        public bool IsMetadata
        {
            get
            {
                // metadata fields start their Path with /Metadata
                return Path.StartsWith("/Metadata");
            }
        }

        /// <summary>
        /// Is field an embedded field?
        /// </summary>
        public bool IsEmbedded
        {
            // TODO this could also be a linked field, does that matter?
            get 
            {
                // path of an embedded field contains more than two forward slashes, 
                // e.g. /Article/articleBody/subheading
                int count = 0;
                foreach (char c in Path)
                {
                    if (c == '/')
                    {
                        count++;
                    }                    
                }
                return count > 2;
            }
        }

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
        /// Check if current field contains given semantics.
        /// </summary>
        /// <param name="fieldSemantics">The semantics to check against</param>
        /// <returns>True if this field contains given semantics, false otherwise</returns>
        public bool ContainsSemantics(FieldSemantics fieldSemantics)
        {
            foreach (var property in Semantics)
            {
                // TODO add proper Equals implementation in FieldSemantics
                if (property.Property.Equals(fieldSemantics.Property) &&
                    property.Prefix.Equals(fieldSemantics.Prefix) /*&& -- removed as this is breaking on embedded fields where the property.Entity is something like EmbeddedLink
                    property.Entity.Equals(fieldSemantics.Entity)*/)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Find <see cref="SemanticSchemaField"/> with given semantics.
        /// </summary>
        /// <param name="fieldSemantics">The semantics to check against</param>
        /// <returns>This field or one of its embedded fields that match with the given semantics, null if a match cannot be found</returns>
        public SemanticSchemaField FindFieldBySemantics(FieldSemantics fieldSemantics)
        {
            if (ContainsSemantics(fieldSemantics))
            {
                return this;
            }

            foreach (var embeddedField in Fields)
            {
                SemanticSchemaField field = embeddedField.FindFieldBySemantics(fieldSemantics);
                if (field != null)
                {
                    return field;
                }
            }
            return null;
        }
    }
}
