using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DD4T.ContentModel;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.Mvc.Models;

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
            Dictionary<string, string> propertyData = new Dictionary<string, string>();
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

                // get schema entity names (indexed by vocabulary)
                SemanticSchema semanticSchema = SemanticMapping.GetSchema(schemaId);
                ILookup<string, string> entityNames = semanticSchema.GetEntityNames();

                var model = Activator.CreateInstance(type);
                Dictionary<string, string> vocabularies = GetVocabulariesFromType(type);
                var propertySemantics = LoadPropertySemantics(type);
                foreach (var pi in type.GetProperties())
                {
                    if (propertySemantics.ContainsKey(pi.Name))
                    {
                        foreach (var info in propertySemantics[pi.Name])
                        {
                            // semantic mapping of fields
                            string fieldname;

                            // get vocabulary for prefix (use default vocabulary if not available)
                            string vocab = SemanticMapping.DefaultVocabulary;
                            if (!string.IsNullOrEmpty(info.Prefix) && vocabularies.ContainsKey(info.Prefix))
                            {
                                vocab = vocabularies[info.Prefix];
                            }
                            else if (string.IsNullOrEmpty(info.Prefix) && vocabularies.ContainsKey(string.Empty))
                            {
                                vocab = vocabularies[string.Empty];
                            }

                            // determine field semantics
                            string prefix = SemanticMapping.GetPrefix(vocab);
                            string property = info.PropertyName;
                            string entity = entityNames[vocab].First();
                            FieldSemantics fieldSemantics = new FieldSemantics(prefix, entity, property);

                            // locate semantic schema field
                            SemanticSchemaField matchingField = semanticSchema.FindFieldBySemantics(fieldSemantics);  
                            if (matchingField != null)
                            {
                                // we found a field with given semantics
                                fieldname = matchingField.Name;
                            }
                            else
                            {
                                // we did not find a field with given semantics, do basic property name -> xml field mapping
                                fieldname = info.PropertyName;
                            }

                            bool multival = pi.PropertyType.IsGenericType && (pi.PropertyType.GetGenericTypeDefinition() == typeof(List<>));
                            // TODO remove multivalue and image hacks
                            if (multival && !component.Fields.ContainsKey(fieldname))
                            {
                                // truncate multivalue properties by one character as the Tridion field name is usually singular (eg link instead of links)
                                fieldname = fieldname.Substring(0, fieldname.Length - 1);
                            }
                            //Hack - for mapping images to the Image property - this should use some semantics
                            if (fieldname=="image" && component.ComponentType == ComponentType.Multimedia)
                            {
                                pi.SetValue(model, GetImages(new List<IComponent>{component})[0]);
                            }

                            // determine type
                            Type propertyType = multival ? pi.PropertyType.GetGenericArguments()[0] : pi.PropertyType;

                            // try getting value from content fields
                            if (component.Fields.ContainsKey(fieldname))
                            {
                                var field = component.Fields[fieldname];
                                pi.SetValue(model, GetFieldValues(field, propertyType, multival));
                                propertyData.Add(pi.Name, GetFieldXPath(field));
                                // we found a field, we are done - no need to process other semantics for this property
                                break;
                            }

                            // try getting value from metadata fields
                            if (component.MetadataFields.ContainsKey(fieldname))
                            {
                                var field = component.MetadataFields[fieldname];
                                pi.SetValue(model, GetFieldValues(field, propertyType, multival));
                                propertyData.Add(pi.Name, GetFieldXPath(field));
                                // we found a field, we are done - no need to process other semantics for this property
                                break;
                            }
                            if (matchingField != null && matchingField.IsEmbedded)
                            {
                                // we are dealing with an embedded field
                                // TODO get embedded value 


                                if (matchingField.IsMetadata)
                                {
                                    // we are dealing with an embedded metadata field                                    
                                    // TODO get embedded metadata value

                                }
                            }
                            // TODO if semantic field could not be matched, this could still be an embedded (metadata) field

                        }
                    }
                }
                if (model is Entity)
                {
                    ((Entity)model).EntityData = entityData;
                    ((Entity)model).PropertyData = propertyData;
                }
                return model;

            }
            return null;
        }

        private object GetFieldValues(IField field, Type propertyType, bool multival)
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
                    return GetMultiEmbedded(field, propertyType, multival);
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
            if (typeof(DateTime).IsAssignableFrom(modelType))
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
            if (typeof(Double).IsAssignableFrom(modelType))
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
            if (typeof(Image).IsAssignableFrom(modelType))
            {
                if (multival)
                {
                    return GetImages(field.LinkedComponentValues);
                }

                return GetImages(field.LinkedComponentValues)[0];
            }
            return null;
        }


        private object GetMultiComponentLinks(IField field, Type linkedItemType, bool multival)
        {
            //TODO is reflection the only way to do this?
            MethodInfo method = GetType().GetMethod("GetCompLink" + (multival ? "s" : ""), BindingFlags.NonPublic | BindingFlags.Instance);
            method = method.MakeGenericMethod(new[] { linkedItemType });
            return method.Invoke(this,new object[]{field.LinkedComponentValues,linkedItemType});
        }

        private object GetMultiEmbedded(IField field, Type propertyType, bool multival)
        {
            //TODO is there some way we can make this more generic using semantics?
            if (propertyType == typeof(Link))
            {
                var links = GetLinks(field.EmbeddedValues);
                if (multival)
                {
                    return links;
                }

                return links.Count > 0 ? links[0] : null;
            }
            if (propertyType == typeof(Paragraph))
            {
                var paras = GetParagraphs(field.EmbeddedValues);
                if (multival)
                {
                    return paras;
                }

                return paras.Count > 0 ? paras[0] : null;
            }
            return null;
        }

        private static object GetStrings(IField field, Type modelType, bool multival)
        {
            if (typeof(String).IsAssignableFrom(modelType))
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

        private List<Link> GetLinks(IEnumerable<IFieldSet> list)
        {
            var result = new List<Link>();
            foreach (IFieldSet fs in list)
            {
                var link = new Link
                    {
                        AlternateText = fs.ContainsKey("alternateText") ? fs["alternateText"].Value : null,
                        LinkText = fs.ContainsKey("linkText") ? fs["linkText"].Value : null,
                        Url = fs.ContainsKey("externalLink") ? fs["externalLink"].Value : (fs.ContainsKey("internalLink") ? LinkFactory.ResolveExtensionlessLink(fs["internalLink"].LinkedComponentValues[0].Id) : null)
                    };
                if (!String.IsNullOrEmpty(link.Url))
                {
                    result.Add(link);
                }
            }
            return result;
        }

        private static List<Paragraph> GetParagraphs(IEnumerable<IFieldSet> list)
        {
            var result = new List<Paragraph>();
            foreach (IFieldSet fs in list)
            {
                var para = new Paragraph
                    {
                        Subheading = fs.ContainsKey("subheading") ? fs["subheading"].Value : null,
                        Content = fs.ContainsKey("content") ? fs["content"].Value : null,
                        // TODO for now we assume its an image - needs generic treatment
                        Media = fs.ContainsKey("media") ? GetImages(new List<IComponent> { fs["media"].LinkedComponentValues[0] })[0] : null,
                        Caption = fs.ContainsKey("caption") ? fs["caption"].Value : null
                    };
                result.Add(para);
            }
            return result;
        }
    }
}
