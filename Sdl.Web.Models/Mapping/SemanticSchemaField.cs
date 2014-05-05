using System.Collections.Generic;

namespace Sdl.Web.Mvc.Mapping
{
    public class SemanticSchemaField
    {
        // {"name":"headline","isMultiValue":false,"semantics":[{"vocab":"s","property":"headline"}],"fields":[]}
        public string name { get; set; }
        public bool isMultiValue { get; set; }
        public List<SemanticProperty> semantics { get; set; }
        public List<SemanticSchemaField> fields { get; set; }

        /// <summary>
        /// Check if current field contains given semantic property
        /// </summary>
        /// <param name="semanticProperty">the semantic property to check against</param>
        /// <returns>true if current field contains a combination of semanticProperty.vocab and semanticProperty.property, false otherwise</returns>
        public bool Contains(SemanticProperty semanticProperty)
        {
            foreach (var property in semantics)
            {
                if (property.Equals(semanticProperty))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Find SemanticSchemaField with given semantic property
        /// </summary>
        /// <param name="semanticProperty">the semantic property to check against</param>
        /// <returns>current field or one of its sub fields that match with the given semantic property</returns>
        public SemanticSchemaField Find(SemanticProperty semanticProperty)
        {
            if (Contains(semanticProperty))
            {
                return this;
            }

            foreach (var subField in fields)
            {
                SemanticSchemaField field = subField.Find(semanticProperty);
                if (field != null)
                {
                    return field;
                }
            }
            return null;
        }
    }
}
