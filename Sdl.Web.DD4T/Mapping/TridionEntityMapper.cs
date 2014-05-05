using System.Collections.Generic;
using DD4T.ContentModel;
using Sdl.Web.Mvc.Mapping;

namespace Sdl.Web.DD4T.Mapping
{
    public class TridionEntityMapper : IEntityMapper
    {
        public string GetPropertyValue(object sourceEntity, List<SemanticProperty> properties)
        {
            IComponent component = ((IComponentPresentation)sourceEntity).Component;

            // tcm:0-1
            string[] uriParts = component.Schema.Id.Split('-');

            // get semantic mappings for fields from schema
            // TODO load schemas from json and find current schema by its item id
            SemanticSchema schema = new SemanticSchema { Id = uriParts[1]};

            foreach (var semanticProperty in properties)
            {
                // find schema field that matches "vocab" = semanticProperty.Vocab && "property" = semanticProperty.PropertyName
                var matchingField = schema.Find(semanticProperty);
                if (matchingField != null && component.Fields.ContainsKey(matchingField.Name))
                {
                    return component.Fields[matchingField.Name].Value;
                }
            }

            return null;
        }
    }
}
