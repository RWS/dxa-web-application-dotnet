using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Mapping
{
    public abstract class BaseModelBuilder
    {
        private static readonly object SemanticsLock = new object();

        private static Dictionary<Type, Dictionary<string, List<SemanticProperty>>> _entityPropertySemantics;
        public static Dictionary<Type, Dictionary<string, List<SemanticProperty>>> EntityPropertySemantics 
        {
            get
            {
                if (_entityPropertySemantics == null)
                {
                    _entityPropertySemantics = new Dictionary<Type, Dictionary<string, List<SemanticProperty>>>();
                }
                return _entityPropertySemantics;
            }
            set
            {
                _entityPropertySemantics = value;
            }
        }        

        protected virtual Dictionary<string, KeyValuePair<string, string>> GetEntityDataFromType(Type type)
        {
            Dictionary<string, KeyValuePair<string, string>> res = new Dictionary<string, KeyValuePair<string, string>>();
            foreach (Attribute attr in type.GetCustomAttributes())
            {
                if (attr is SemanticEntityAttribute)
                {
                    SemanticEntityAttribute semantics = (SemanticEntityAttribute)attr;
                    // we can only support mapping to a single semantic entity, the derived type is set first, so that is what we use
                    if (!res.ContainsKey(semantics.Prefix))
                    {
                        res.Add(semantics.Prefix, new KeyValuePair<string, string>(semantics.Vocab, semantics.EntityName));                        
                    }
                }
                if (attr is SemanticDefaultsAttribute)
                {
                    SemanticDefaultsAttribute semantics = (SemanticDefaultsAttribute)attr;
                    res.Add(semantics.Prefix, new KeyValuePair<string, string>(semantics.Vocab, String.Empty));
                }
            }

            //Add default mapping if none was specified on entity
            if (!res.ContainsKey(string.Empty))
            {
                res.Add(string.Empty, new KeyValuePair<string, string>(ViewModel.CoreVocabulary, string.Empty));
            }

            return res;
        }

        protected virtual Dictionary<string, string> GetEntitiesFromType(Type type)
        {
            Dictionary<string, string> res = new Dictionary<string, string>();
            foreach (Attribute attr in type.GetCustomAttributes())
            {
                if (attr is SemanticEntityAttribute)
                {
                    SemanticEntityAttribute semantics = (SemanticEntityAttribute)attr;
                    res.Add(semantics.Prefix, semantics.EntityName);
                }
            }
            return res;
        }

        protected virtual Dictionary<string, List<SemanticProperty>> LoadPropertySemantics(Type type)
        {
            lock (SemanticsLock)
            {
                if (!EntityPropertySemantics.ContainsKey(type))
                {
                    //The default prefix is empty - this implies that the prefix should be inherited from the parent object default prefix
                    string defaultPrefix = String.Empty;
                    bool mapAllProperties = true;
                    SemanticDefaultsAttribute semanticDefaultsAttr = type.GetCustomAttributes().OfType<SemanticDefaultsAttribute>().FirstOrDefault();
                    if (semanticDefaultsAttr != null)
                    {
                        defaultPrefix = semanticDefaultsAttr.Prefix;
                        mapAllProperties = semanticDefaultsAttr.MapAllProperties;
                    }

                    Dictionary<string, List<SemanticProperty>> result = new Dictionary<string, List<SemanticProperty>>();
                    foreach (PropertyInfo pi in type.GetProperties())
                    {
                        string name = pi.Name;
                        //flag to indicate we have processed a default mapping, or we explicitly should ignore this property when mapping
                        bool ignore = false;
                        foreach (SemanticPropertyAttribute semanticPropertyAttr in pi.GetCustomAttributes(true).OfType<SemanticPropertyAttribute>())
                        {
                            if (!semanticPropertyAttr.IgnoreMapping)
                            {
                                if (!result.ContainsKey(name))
                                {
                                    result.Add(name, new List<SemanticProperty>());
                                }
                                string[] propertyNameParts = semanticPropertyAttr.PropertyName.Split(':');
                                if (propertyNameParts.Length > 1)
                                {
                                    result[name].Add(new SemanticProperty(propertyNameParts[0], propertyNameParts[1]));
                                }
                                else
                                {
                                    //Add the default prefix and set the ignore flag - so no need to apply default mapping using property name
                                    result[name].Add(new SemanticProperty(defaultPrefix, propertyNameParts[0]));
                                    ignore = true;
                                }
                            }
                            else
                            {
                                ignore = true;
                            }
                        }
                        if (!ignore && mapAllProperties)
                        {
                            if (!result.ContainsKey(name))
                            {
                                result.Add(name, new List<SemanticProperty>());
                            }
                            //Add default semantics 
                            result[name].Add(GetDefaultPropertySemantics(pi, defaultPrefix));
                        }

                    }
                    EntityPropertySemantics.Add(type, result);
                }
            }
            return EntityPropertySemantics[type];
        }

        protected virtual SemanticProperty GetDefaultPropertySemantics(PropertyInfo pi, string defaultPrefix)
        {
            //This is where we map model class property names to Tridion schema xml field names
            //Which is: the property name with the first character lower case and the last character removed if its an 's' and a List<> type (so Paragraphs becomes paragraph etc.)
            string name = pi.Name.Substring(0, 1).ToLower() + pi.Name.Substring(1);
            if (pi.PropertyType.IsGenericType && pi.PropertyType.GetGenericTypeDefinition() == typeof(List<>) && name.EndsWith("s"))
            {
                name = name.Substring(0,name.Length-1);
            }
            return new SemanticProperty(defaultPrefix, name);
        }
    }
}
