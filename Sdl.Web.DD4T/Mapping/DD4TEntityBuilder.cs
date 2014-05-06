using DD4T.ContentModel;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.Mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.DD4T.Mapping
{
    public class DD4TEntityBuilder : BaseEntityBuilder
    {
        public ExtensionlessLinkFactory LinkFactory { get; set; }
        public DD4TEntityBuilder()
        {
            this.LinkFactory = new ExtensionlessLinkFactory();
        }
        
        public override object Create(object sourceEntity,Type type)
        {
            IComponent component = sourceEntity as IComponent;
            Dictionary<string, string> entityData = null;
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
                var model = Activator.CreateInstance(type);
                Dictionary<string, string> vocabularies = GetVocabulariesFromType(type);
                var propertySemantics = LoadPropertySemantics(type);
                foreach (var pi in type.GetProperties())
                {
                    if (propertySemantics.ContainsKey(pi.Name))
                    {
                        foreach (var info in propertySemantics[pi.Name])
                        {
                            bool multival = pi.PropertyType.IsGenericType && (pi.PropertyType.GetGenericTypeDefinition() == typeof(List<>));
                            //Here we would do the mapping, but for now we do basic property name -> xml field mapping
                            var fieldname = info.PropertyName;
                            if (multival && !component.Fields.ContainsKey(info.PropertyName))
                            {
                                //truncate multivalue properties by one character as the Tridion field name is usually singular (eg link instead of links)
                                fieldname = fieldname.Substring(0, fieldname.Length - 1);
                            }
                            //TODO check metadata as well!
                            if (component.Fields.ContainsKey(fieldname))
                            {
                                var field = component.Fields[fieldname];
                                Type propertyType = multival ? pi.PropertyType.GetGenericArguments()[0] : pi.PropertyType;
                                switch (field.FieldType)
                                {
                                    case (FieldType.Date):
                                        pi.SetValue(model, GetDates(field, propertyType, multival));
                                        break;
                                    case (FieldType.Number):
                                        pi.SetValue(model, GetNumbers(field, propertyType, multival));
                                        break;
                                    case (FieldType.MultiMediaLink):
                                        pi.SetValue(model, GetMultiMediaLinks(field, propertyType, multival));
                                        break;
                                    case (FieldType.ComponentLink):
                                        pi.SetValue(model, GetMultiComponentLinks(field, propertyType, multival));
                                        break;
                                    case (FieldType.Embedded):
                                        pi.SetValue(model, GetMultiEmbedded(field, propertyType, multival));
                                        break;
                                    default:
                                        pi.SetValue(model, GetStrings(field, propertyType, multival));
                                        break;
                                }
                                propertyData.Add(pi.Name,GetFieldXPath(field));
                                //If we found a field, we are done - no need to process other semantics for this property
                                break;
                            }
                        }
                        object value = null;
                        if (value != null)
                        {
                            pi.SetValue(model, value);
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

        private string GetFieldXPath(IField field)
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

        private object GetDates(IField field, Type modelType, bool multival)
        {
            if (typeof(DateTime).IsAssignableFrom(modelType))
            {
                if (multival)
                {
                    return field.DateTimeValues;
                }
                else
                {
                    return field.DateTimeValues[0];
                }
            }
            return null;
        }

        private object GetNumbers(IField field, Type modelType, bool multival)
        {
            if (typeof(Double).IsAssignableFrom(modelType))
            {
                if (multival)
                {
                    return field.NumericValues;
                }
                else
                {
                    return field.NumericValues[0];
                }
            }
            return null;
        }

        private object GetMultiMediaLinks(IField field, Type modelType, bool multival)
        {
            if (typeof(Image).IsAssignableFrom(modelType))
            {
                if (multival)
                {
                    return GetImages(field.LinkedComponentValues);
                }
                else
                {
                    return GetImages(field.LinkedComponentValues)[0];
                }
            }
            return null;
        }


        private object GetMultiComponentLinks(IField field, Type linkedItemType, bool multival)
        {
            //TODO is reflection the only way to do this?
            MethodInfo method = GetType().GetMethod("GetCompLink" + (multival ? "s" : ""), BindingFlags.NonPublic | BindingFlags.Instance);
            method = method.MakeGenericMethod(new Type[] { linkedItemType });
            return method.Invoke(this,new object[]{field.LinkedComponentValues,linkedItemType});
        }

        private object GetMultiEmbedded(IField field, Type propertyType, bool multival)
        {
            if (propertyType == typeof(Link))
            {
                var links = GetLinks(field.EmbeddedValues);
                if (multival)
                {
                    return links;
                }
                else
                {
                    return links.Count > 0 ? links[0] : null;
                }
            }
            return null;
        }

        private object GetStrings(IField field, Type modelType, bool multival)
        {
            if (typeof(String).IsAssignableFrom(modelType))
            {
                if (multival)
                {
                    return field.Values;
                }
                else
                {
                    return field.Value;
                }
            }
            return null;
        }

        private List<Image> GetImages(IList<IComponent> components)
        {
            return components.Select(c => new Image { Url = c.Multimedia.Url, Id = c.Id, FileSize = c.Multimedia.Size }).ToList();
        }

        private List<T> GetCompLinks<T>(IList<IComponent> components, Type linkedItemType)
        {
            List<T> list = new List<T>();
            foreach (var comp in components)
            {
                list.Add((T)this.Create(comp, linkedItemType));
            }
            return list;
        }
        private T GetCompLink<T>(IList<IComponent> components, Type linkedItemType)
        {

            return GetCompLinks<T>(components,linkedItemType)[0];
        }

        private List<Link> GetLinks(IList<IFieldSet> list)
        {
            var result = new List<Link>();
            foreach (IFieldSet fs in list)
            {
                var link = new Link();
                link.AlternateText = fs.ContainsKey("alternateText") ? fs["alternateText"].Value : null;
                link.LinkText = fs.ContainsKey("linkText") ? fs["linkText"].Value : null;
                link.Url = fs.ContainsKey("externalLink") ? fs["externalLink"].Value : (fs.ContainsKey("internalLink") ? LinkFactory.ResolveExtensionlessLink(fs["internalLink"].LinkedComponentValues[0].Id) : null);
                if (!String.IsNullOrEmpty(link.Url))
                {
                    result.Add(link);
                }
            }
            return result;
        }
    }
}
