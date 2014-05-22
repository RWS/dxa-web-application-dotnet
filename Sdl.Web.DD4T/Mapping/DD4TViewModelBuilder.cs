using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DD4T.ContentModel;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.Mvc.Models;
using System.Collections;

namespace Sdl.Web.DD4T.Mapping
{
    public class DD4TViewModelBuilder : BaseViewModelBuilder
    {
        public ExtensionlessLinkFactory LinkFactory { get; set; }

        public DD4TViewModelBuilder()
        {
            LinkFactory = new ExtensionlessLinkFactory();
        }
        
        public override object Create(object sourceEntity, Type type)
        {
            IComponent component = sourceEntity as IComponent;
            Dictionary<string, string> entityData;
            if (component == null && sourceEntity is IComponentPresentation)
            {
                var cp = (IComponentPresentation)sourceEntity;
                component = cp.Component;
                entityData = GetEntityData(cp);
            }
            else
            {
                entityData = GetEntityData(component);
            }
            if (component != null)
            {
                // get schema item id from tcmuri -> tcm:1-2-8
                string[] uriParts = component.Schema.Id.Split('-');
                long schemaId = Convert.ToInt64(uriParts[1]);
                MappingData mapData = new MappingData();
                // get schema entity names (indexed by vocabulary)
                mapData.SemanticSchema = SemanticMapping.GetSchema(schemaId);
                mapData.EntityNames = mapData.SemanticSchema.GetEntityNames();
            
                //TODO may need to merge with vocabs from embedded types
                mapData.Vocabularies = GetVocabulariesFromType(type);
                mapData.Content = component.Fields;
                mapData.Meta = component.MetadataFields;
                mapData.TargetType = type;
                mapData.SourceEntity = component;
                var model = CreateModelFromMapData(mapData);
                if (model is Entity)
                {
                    ((Entity)model).EntityData = entityData;
                }
                return model;

            }
            return null;
        }

        protected virtual object CreateModelFromMapData(MappingData mapData)
        {
            var model = Activator.CreateInstance(mapData.TargetType);
            Dictionary<string, string> propertyData = new Dictionary<string, string>();
            var propertySemantics = LoadPropertySemantics(mapData.TargetType);
            foreach (var pi in mapData.TargetType.GetProperties())
            {
                bool multival = pi.PropertyType.IsGenericType && (pi.PropertyType.GetGenericTypeDefinition() == typeof(List<>));
                Type propertyType = multival ? pi.PropertyType.GetGenericArguments()[0] : pi.PropertyType;
                if (propertySemantics.ContainsKey(pi.Name))
                {
                    foreach (var info in propertySemantics[pi.Name])
                    {
                        IField field = GetFieldFromSemantics(mapData, info);
                        if (field != null && (field.Values.Count > 0 || field.EmbeddedValues.Count > 0))
                        {
                            pi.SetValue(model, GetFieldValues(field, propertyType, multival, mapData));
                            propertyData.Add(pi.Name, GetFieldXPath(field));
                            break;
                        }
                        //Special cases, where we want to map for example the whole entity to an image property, or a resolved link to the entity to a Url field
                        if (info.PropertyName == "_self")
                        {
                            bool processed = false;
                            switch(pi.Name)
                            {
                                case "Image":
                                    if (mapData.SourceEntity!=null && mapData.SourceEntity.Multimedia!=null)
                                    {
                                        pi.SetValue(model, GetMultiMediaLinks(new List<IComponent> { mapData.SourceEntity }, propertyType, multival));
                                        processed = true;
                                    }
                                    break;
                                case "Link":
                                    if (mapData.SourceEntity != null)
                                    {
                                        pi.SetValue(model, GetMultiComponentLinks(new List<IComponent> { mapData.SourceEntity }, propertyType, multival));
                                    }
                                    break;
                            }
                            if (processed)
                            {
                                break;
                            }
                        }   
                    }
                }
            }
            if (model is Entity)
            {
                ((Entity)model).PropertyData = propertyData;
            }
            return model;
        }

        private IField GetFieldFromSemantics(MappingData mapData, SemanticProperty info)
        {
            string lookup = info.Prefix;
            if (mapData.ParentDefaultPrefix != null && String.IsNullOrEmpty(info.Prefix))
            {
                lookup = mapData.ParentDefaultPrefix;
            }
            if (mapData.Vocabularies.ContainsKey(lookup))
            {
                //TODO check exists
                var vocab = mapData.Vocabularies[lookup];
                // semantic mapping of fields
                string fieldname = null;

                // determine field semantics
                string prefix = SemanticMapping.GetPrefix(vocab);
                string property = info.PropertyName;
                string entity = mapData.EntityNames[vocab].First();
                FieldSemantics fieldSemantics = new FieldSemantics(prefix, entity, property);

                // locate semantic schema field
                SemanticSchemaField matchingField = mapData.SemanticSchema.FindFieldBySemantics(fieldSemantics);
                if (matchingField != null)
                {
                    return ExtractMatchedField(matchingField, matchingField.IsMetadata ? mapData.Meta : mapData.Content, mapData.EmbedLevel);
                }
            }
            return null;
        }

        private IField ExtractMatchedField(SemanticSchemaField matchingField, IFieldSet fields, int embedLevel, string path = null)
        {
            if (path==null)
            {
                path = matchingField.Path;
                while (embedLevel >= -1 && path.Contains("/"))
                {
                    int pos = path.IndexOf("/");
                    path = path.Substring(pos+1);
                    embedLevel--;
                }
            }
            var bits = path.Split('/');
            if (fields.ContainsKey(bits[0]))
            {
                if (bits.Length > 1)
                {
                    int pos = path.IndexOf("/");
                    return ExtractMatchedField(matchingField, fields[bits[0]].EmbeddedValues[0], embedLevel, path.Substring(pos + 1));
                }
                else
                {
                    return fields[bits[0]];
                }
            }
            return null;
        }

        private object GetFieldValues(IField field, Type propertyType, bool multival, MappingData mapData)
        {
            switch (field.FieldType)
            {
                case (FieldType.Date):
                    return GetDates(field, propertyType, multival);
                case (FieldType.Number):
                    return GetNumbers(field, propertyType, multival);
                case (FieldType.MultiMediaLink):
                    return GetMultiMediaLinks(field, propertyType, multival);
                case (FieldType.ComponentLink):
                    return GetMultiComponentLinks(field, propertyType, multival);
                case (FieldType.Embedded):
                    return GetMultiEmbedded(field, propertyType, multival, mapData);
                default:
                    return GetStrings(field, propertyType, multival);
            }
        }

        private static string GetFieldXPath(IField field)
        {
            return field.XPath;
        }

        protected virtual Dictionary<string, string> GetEntityData(IComponent comp)
        {
            var res = new Dictionary<string, string>();
            if (comp != null)
            {
                res.Add("ComponentID", comp.Id);
                res.Add("ComponentModified", comp.RevisionDate.ToString("s"));
            }
            return res;
        }

        protected virtual Dictionary<string, string> GetEntityData(IComponentPresentation cp)
        {
            if (cp != null)
            {
                var res = GetEntityData(cp.Component);
                res.Add("ComponentTemplateID", cp.ComponentTemplate.Id);
                res.Add("ComponentTemplateModified", cp.ComponentTemplate.RevisionDate.ToString("s"));
                return res;
            }
            return new Dictionary<string, string>();
        }

        private static object GetDates(IField field, Type modelType, bool multival)
        {
            if (modelType.IsAssignableFrom(typeof(DateTime)))
            {
                if (multival)
                {
                    return field.DateTimeValues;
                }

                return field.DateTimeValues[0];
            }
            return null;
        }

        private static object GetNumbers(IField field, Type modelType, bool multival)
        {
            if (modelType.IsAssignableFrom(typeof(Double)))
            {
                if (multival)
                {
                    return field.NumericValues;
                }

                return field.NumericValues[0];
            }
            return null;
        }

        private static object GetMultiMediaLinks(IField field, Type modelType, bool multival)
        {
            return GetMultiMediaLinks(field.LinkedComponentValues, modelType, multival);
        }


        private static object GetMultiMediaLinks(IList<IComponent> items, Type modelType, bool multival)
        {
            //TODO, handle other types
            if (modelType.IsAssignableFrom(typeof(Image)))
            {
                if (multival)
                {
                    return GetImages(items);
                }

                return GetImages(items)[0];
            }
            return null;
        }


        private object GetMultiComponentLinks(IField field, Type linkedItemType, bool multival)
        {
            return GetMultiComponentLinks(field.LinkedComponentValues, linkedItemType, multival);
        }

        private object GetMultiComponentLinks(IList<IComponent> items, Type linkedItemType, bool multival)
        {
            //What to do depends on the target type
            if (linkedItemType == typeof(String) || linkedItemType == typeof(Link))
            {
                //For strings and Links, we simply resolve the link to a URL
                List<String> urls = new List<String>();
                foreach (var comp in items)
                {
                    var url = LinkFactory.ResolveExtensionlessLink(comp.Id);
                    if (url != null)
                    {
                        urls.Add(url);
                    }
                }
                if (urls.Count == 0)
                {
                    return null;
                }
                if (linkedItemType == typeof(Link))
                {
                    if (multival)
                    {
                        return urls.Select(u => new Link { Url = u }).ToList();
                    }
                    return new Link { Url = urls[0] };
                }
                else if (multival)
                {
                    return urls;
                }
                else
                {
                    return urls[0];
                }
            }
            else
            {
                //TODO is reflection the only way to do this?
                MethodInfo method = GetType().GetMethod("GetCompLink" + (multival ? "s" : ""), BindingFlags.NonPublic | BindingFlags.Instance);
                method = method.MakeGenericMethod(new[] { linkedItemType });
                return method.Invoke(this, new object[] { items, linkedItemType });
            }
        }

        private object GetMultiEmbedded(IField field, Type propertyType, bool multival, MappingData mapData)
        {
            MappingData embedMapData = new MappingData();
            embedMapData.TargetType = propertyType;
            embedMapData.Meta = null;
            embedMapData.EntityNames = mapData.EntityNames;
            embedMapData.ParentDefaultPrefix = mapData.ParentDefaultPrefix;
            embedMapData.Vocabularies = mapData.Vocabularies;
            embedMapData.SemanticSchema = mapData.SemanticSchema;
            embedMapData.EmbedLevel = mapData.EmbedLevel + 1;
            //This is a bit weird, but necessary as we cannot return List<object>, we need to get the right type
            var result = (IList)typeof(List<>).MakeGenericType(propertyType).GetConstructor(Type.EmptyTypes).Invoke(null);
            foreach (IFieldSet fields in field.EmbeddedValues)
            {
                embedMapData.Content = fields;
                var model = CreateModelFromMapData(embedMapData);
                if (model != null)
                {
                    result.Add(model);
                }
            }
            return result.Count == 0 ? null : (multival ? result : result[0]);

        }

        private static object GetStrings(IField field, Type modelType, bool multival)
        {
            if (modelType.IsAssignableFrom(typeof(String)))
            {
                if (multival)
                {
                    return field.Values;
                }

                return field.Value;
            }
            return null;
        }

        private static List<Image> GetImages(IEnumerable<IComponent> components)
        {
            return components.Select(c => new Image { Url = c.Multimedia.Url, FileSize = c.Multimedia.Size }).ToList();
        }

        private List<T> GetCompLinks<T>(IEnumerable<IComponent> components, Type linkedItemType)
        {
            List<T> list = new List<T>();
            foreach (var comp in components)
            {
                list.Add((T)Create(comp, linkedItemType));
            }
            return list;
        }

        private T GetCompLink<T>(IEnumerable<IComponent> components, Type linkedItemType)
        {
            return GetCompLinks<T>(components,linkedItemType)[0];
        }
    }
}
