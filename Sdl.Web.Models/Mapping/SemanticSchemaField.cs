using System.Collections.Generic;

namespace Sdl.Web.Mvc.Mapping
{
    public class SemanticSchemaField
    {
        // {"name":"headline","isMultiValue":false,"semantics":[{"vocab":"s","property":"headline"}],"fields":[]}
        public string Name { get; set; }
        public bool IsMultiValue { get; set; }
        public List<SemanticProperty> Semantics { get; set; }
        public List<SemanticSchemaField> Fields { get; set; }

        /// <summary>
        /// Check if current field contains given semantic property
        /// </summary>
        /// <param name="semanticProperty">the semantic property to check against</param>
        /// <returns>true if current field contains a combination of semanticProperty.Vocab and semanticProperty.PropertyName, false otherwise</returns>
        public bool Contains(SemanticProperty semanticProperty)
        {
            foreach (var property in Semantics)
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

            foreach (var subField in Fields)
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
