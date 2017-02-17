using System;
using System.Collections.Generic;
using System.Reflection;
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
                result = ModelTypeRegistry.GetPropertySemantics(type);
                _semanticPropertiesCache.Add(type, result);
                return result;
            }
        }
    }
}
