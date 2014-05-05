using System.Collections.Generic;

namespace Sdl.Web.Mvc.Mapping
{
    public class SemanticSchema
    {
        // {"id":80,"rootElement":"Article","fields":[{"name":"headline","isMultiValue":false,"semantics":[{"vocab":"s","property":"headline"}],"fields":[]},{"name":"image","isMultiValue":false,"semantics":[{"vocab":"s","property":"image"}],"fields":[]},{"name":"articleBody","isMultiValue":false,"semantics":[{"vocab":"s","property":"articleBody"}],"fields":[]}],"semantics":[{"vocab":"s","entity":"Article"}]}
        public string Id { get; set; }
        public string RootElement { get; set; }
        public List<SemanticSchemaField> Fields { get; set; }
        public List<SemanticProperty> Semantics { get; set; }

        /// <summary>
        /// Find SemanticSchemaField with given semantic property
        /// </summary>
        /// <param name="semanticProperty">the semantic property to check against</param>
        /// <returns>schema field or one of its sub fields that match with the given semantic property</returns>
        public SemanticSchemaField Find(SemanticProperty semanticProperty)
        {
            foreach (var field in Fields)
            {
                var matchingField = field.Find(semanticProperty);
                if (matchingField != null)
                {
                    return matchingField;
                }
            }

            return null;
        }
    }
}
