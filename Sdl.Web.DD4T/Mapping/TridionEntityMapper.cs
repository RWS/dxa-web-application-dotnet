using System;
using System.Collections.Generic;
using DD4T.ContentModel;
using Sdl.Web.Mvc.Mapping;

namespace Sdl.Web.DD4T.Mapping
{
    public class TridionEntityMapper : IEntityMapper
    {
        public object GetPropertyValue(object sourceEntity, List<SemanticFieldProperty> properties)
        {
            IComponent component = ((IComponentPresentation)sourceEntity).Component;

            // tcm:0-1
            string[] uriParts = component.Schema.Id.Split('-');
            long schemaId = Convert.ToInt64(uriParts[1]);

            // get semantic mappings for fields from schema
            // TODO load schemas from json and find current schema by its item id
            SemanticSchema schema = new SemanticSchema { Id = schemaId };

            foreach (var semanticProperty in properties)
            {
                // find schema field that matches "vocab" = semanticProperty.vocab && "property" = semanticProperty.property
                var matchingField = schema.Find(semanticProperty);
                if (matchingField != null && component.Fields.ContainsKey(matchingField.Name))
                {
                    IField field = component.Fields[matchingField.Name];

                    // TODO return correct index from possible multiple values
                    switch (field.FieldType)
                    {
                        case FieldType.Number:
                            return field.NumericValues[0];
                        case FieldType.Date:
                            return field.DateTimeValues[0];
                        case FieldType.ComponentLink:
                            return field.LinkedComponentValues[0];
                        case FieldType.Keyword:
                            return field.Keywords[0];
                        default:
                            return field.Value;
                    }
                }
            }

            return null;
        }
    }
}
