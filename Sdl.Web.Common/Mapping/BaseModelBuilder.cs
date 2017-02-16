using System;
using System.Collections.Generic;
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
            // TODO TSI-878: rewire to ModelTypeRegistry.GetPropertySemantics.
            lock (_semanticPropertiesCache)
            {
                // Try to get cached semantics
                Dictionary<string, List<SemanticProperty>> result;
                if (_semanticPropertiesCache.TryGetValue(type, out result))
                {
                    return result;
                }

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
                    bool useImplicitMapping = mapAllProperties;
                    List<SemanticProperty> semanticProperties = new List<SemanticProperty>();
                    foreach (SemanticPropertyAttribute semanticPropertyAttr in property.GetCustomAttributes<SemanticPropertyAttribute>(true))
                    {
                        if (semanticPropertyAttr.IgnoreMapping)
                        {
                            ignoreMapping = true;
                            break;
                        }

                        string[] semanticPropertyNameParts = semanticPropertyAttr.PropertyName.Split(':');
                        if (semanticPropertyAttr.Prefix != null)
                        {
                            semanticProperties.Add(new SemanticProperty(semanticPropertyAttr.Prefix, semanticPropertyAttr.PropertyName));
                        }
                        else
                        {
                            semanticProperties.Add(new SemanticProperty(defaultPrefix, semanticPropertyAttr.PropertyName));
                            useImplicitMapping = false;
                        }
                    }

                    if (useImplicitMapping)
                    {
                        semanticProperties.Add(GetDefaultPropertySemantics(property, defaultPrefix));
                    }

                    if (ignoreMapping || semanticProperties.Count == 0)
                    {
                        continue;
                    }

                    if (result.ContainsKey(propertyName))
                    {
                        // Properties with same name can exist is a property is reintroduced with a different signature in a subclass.
                        Log.Debug("Property with name '{0}' is declared multiple times in type {1}.", propertyName, type.FullName);
                        continue;
                    }

                    result.Add(propertyName, semanticProperties);
                }

                _semanticPropertiesCache.Add(type, result);

                return result;
            }
        }

        protected virtual SemanticProperty GetDefaultPropertySemantics(PropertyInfo property, string defaultPrefix)
        {
            // Transform Pascal case into camel case.
            string semanticPropertyName = property.Name.Substring(0, 1).ToLower() + property.Name.Substring(1);
            if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>) && semanticPropertyName.EndsWith("s"))
            {
                // Remove trailing 's' of List property name
                semanticPropertyName = semanticPropertyName.Substring(0, semanticPropertyName.Length-1);
            }
            return new SemanticProperty(defaultPrefix, semanticPropertyName);
        }
    }
}
