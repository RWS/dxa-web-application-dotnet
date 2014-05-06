using System.Collections.Generic;

namespace Sdl.Web.Mvc.Mapping
{
    public class SemanticSchema
    {
        // {"Id":80,"RootElement":"Article","Fields":[{"Name":"headline","IsMultiValue":false,"Semantics":[{"Prefix":"s","Entity":"Aricle","Property":"headline"}],"Fields":[]},{"Name":"image","IsMultiValue":false,"Semantics":[{"Prefix":"s","Entity":"Article","Property":"image"}],"Fields":[]},{"Name":"articleBody","IsMultiValue":false,"Semantics":[{"Prefix":"s","Entity":"Article","Property":"articleBody"}],"Fields":[]}],"Semantics":[{"Prefix":"s","Entity":"Article"}]}
        public long Id { get; set; }
        public string RootElement { get; set; }
        public List<SemanticSchemaField> Fields { get; set; }
        public List<SchemaSemantics> Semantics { get; set; }

        /// <summary>
        /// Find SemanticSchemaField with given semantic property
        /// </summary>
        /// <param name="fieldSemantics">the semantic property to check against</param>
        /// <returns>schema field or one of its sub fields that match with the given semantic property</returns>
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
