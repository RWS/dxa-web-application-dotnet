using System.Collections.Generic;

namespace Sdl.Web.Mvc.Mapping
{
    public class SemanticSchema
    {
        // {"Id":80,"RootElement":"Article","Fields":[{"Name":"headline","IsMultiValue":false,"Semantics":[{"Vocab":"s","Property":"headline"}],"Fields":[]},{"Name":"image","IsMultiValue":false,"Semantics":[{"Vocab":"s","Property":"image"}],"Fields":[]},{"Name":"articleBody","IsMultiValue":false,"Semantics":[{"Vocab":"s","Property":"articleBody"}],"Fields":[]}],"Semantics":[{"Vocab":"s","Entity":"Article"}]}
        public long Id { get; set; }
        public string RootElement { get; set; }
        public List<SemanticSchemaField> Fields { get; set; }
        public List<SemanticSchemaEntity> Semantics { get; set; }

        /// <summary>
        /// FindFieldByProperty SemanticSchemaField with given semantic property
        /// </summary>
        /// <param name="semanticProperty">the semantic property to check against</param>
        /// <returns>schema field or one of its sub fields that match with the given semantic property</returns>
        public SemanticSchemaField FindFieldByProperty(SemanticFieldProperty semanticProperty)
        {
            foreach (var field in Fields)
            {
                var matchingField = field.FindFieldByProperty(semanticProperty);
                if (matchingField != null)
                {
                    return matchingField;
                }
            }

            return null;
        }
    }
}
