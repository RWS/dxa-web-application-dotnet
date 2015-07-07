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
            bool addedDefaults = false;
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
                    addedDefaults = true;
                }
            }
            //Add default vocab if none was specified on entity
            if (!addedDefaults)
            {
                SemanticDefaultsAttribute semantics = new SemanticDefaultsAttribute();
                res.Add(semantics.Prefix, new KeyValuePair<string, string>(semantics.Vocab, String.Empty));
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

        /// <summary>
        /// Determine a Model Type based on semantic mappings (and a given base model type).
        /// </summary>
        /// <param name="semanticSchema">The semantic Schema representing the source data.</param>
        /// <param name="baseModelType">The base type as obtained from the View Model.</param>
        /// <returns>The given base Model Type or a subclass if a more specific class can be resolved via semantic mapping.</returns>
        /// <remarks>
        /// This method makes it possible (for example) to let the <see cref="Teaser.Media"/> property get an instance of <see cref="Image"/> 
        /// rather than just <see cref="MediaItem"/> (the type of the View Model property).
        /// </remarks>
        protected static Type GetModelTypeFromSemanticMapping(SemanticSchema semanticSchema, Type baseModelType)
        {
            Type[] foundAmbiguousMappings = null;
            string[] semanticTypeNames = semanticSchema.GetSemanticTypeNames();
            foreach (string semanticTypeName in semanticTypeNames)
            {
                IEnumerable<Type> mappedModelTypes = ModelTypeRegistry.GetMappedModelTypes(semanticTypeName);
                if (mappedModelTypes == null)
                {
                    continue;
                }

                Type[] matchingModelTypes = mappedModelTypes.Where(t => baseModelType.IsAssignableFrom(t)).ToArray();
                if (matchingModelTypes.Length == 1)
                {
                    // Exactly one matching model type; return it.
                    return matchingModelTypes[0];
                }
                else if (matchingModelTypes.Length > 1)
                {
                    // Multiple candidate models types found. Continue scanning; maybe we'll find a unique one for another semantic type.
                    foundAmbiguousMappings = matchingModelTypes;
                }
            }

            if (foundAmbiguousMappings == null)
            {
                Log.Warn("No semantic mapping found between Schema {0} ({1}) and model type '{2}'. Sticking with model type.",
                        semanticSchema.Id, String.Join(", ", semanticTypeNames), baseModelType.FullName);
            }
            else
            {
                Log.Warn("Ambiguous semantic mappings found between Schema {0} ({1}) and model type '{2}'. Found types: {3}. Sticking with model type.",
                        semanticSchema.Id, String.Join(", ", semanticTypeNames), String.Join(", ", foundAmbiguousMappings.Select(t => t.FullName)), baseModelType.FullName);
            }

            return baseModelType;
        }
    }
}
