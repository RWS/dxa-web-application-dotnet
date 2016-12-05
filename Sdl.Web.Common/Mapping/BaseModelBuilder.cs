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
        private static readonly Dictionary<Type, Dictionary<string, List<SemanticProperty>>> _semanticPropertiesCache = 
            new Dictionary<Type, Dictionary<string, List<SemanticProperty>>>();

        [Obsolete("Deprecated in DXA 1.7.")]
        public static Dictionary<Type, Dictionary<string, List<SemanticProperty>>> EntityPropertySemantics 
        {
            get
            {
                return _semanticPropertiesCache;
            }
            set
            {
                throw new NotSupportedException("Setting this property is not supported in DXA 1.7.");
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
            lock (_semanticPropertiesCache)
            {
                // Try to get cached semantics
                Dictionary<string, List<SemanticProperty>> result;
                if (_semanticPropertiesCache.TryGetValue(type, out result))
                {
                    return result;
                }

                //The default prefix is empty - this implies that the prefix should be inherited from the parent object default prefix
                string defaultPrefix;
                bool mapAllProperties;
                SemanticDefaultsAttribute semanticDefaultsAttr = type.GetCustomAttribute<SemanticDefaultsAttribute>();
                if (semanticDefaultsAttr == null)
                {
                    defaultPrefix = string.Empty;
                    mapAllProperties = true;
                }
                else
                {
                    defaultPrefix = semanticDefaultsAttr.Prefix;
                    mapAllProperties = semanticDefaultsAttr.MapAllProperties;
                }

                result = new Dictionary<string, List<SemanticProperty>>();
                foreach (PropertyInfo property in type.GetProperties())
                {
                    string propertyName = property.Name;
                    bool ignoreMapping = false;
                    bool addDefaultSemantics = mapAllProperties;
                    List<SemanticProperty> semanticProperties = new List<SemanticProperty>();
                    foreach (SemanticPropertyAttribute semanticPropertyAttr in property.GetCustomAttributes<SemanticPropertyAttribute>(true))
                    {
                        if (semanticPropertyAttr.IgnoreMapping)
                        {
                            ignoreMapping = true;
                            break;
                        }

                        string[] semanticPropertyNameParts = semanticPropertyAttr.PropertyName.Split(':');
                        if (semanticPropertyNameParts.Length > 1)
                        {
                            semanticProperties.Add(new SemanticProperty(semanticPropertyNameParts[0], semanticPropertyNameParts[1]));
                        }
                        else
                        {
                            //Add the default prefix and set the ignore flag - so no need to apply default mapping using property name
                            semanticProperties.Add(new SemanticProperty(defaultPrefix, semanticPropertyNameParts[0]));
                            addDefaultSemantics = false;
                        }
                    }

                    if (ignoreMapping)
                    {
                        continue;
                    }

                    if (addDefaultSemantics)
                    {
                        semanticProperties.Add(GetDefaultPropertySemantics(property, defaultPrefix));
                    }

                    result.Add(propertyName, semanticProperties);
                }

                _semanticPropertiesCache.Add(type, result);

                return result;
            }
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
