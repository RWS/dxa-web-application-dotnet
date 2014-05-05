using Sdl.Web.Mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc.Mapping
{
    public abstract class BaseEntityBuilder : IEntityBuilder
    {
        private static Dictionary<Type, Dictionary<string, List<SemanticProperty>>> _entityPropertySemantics = null;
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
        
        public abstract object Create(object sourceEntity, Type type);

        protected virtual Dictionary<string, string> GetVocabulariesFromType(Type type)
        {
            Dictionary<string,string> res = new Dictionary<string,string>();
            foreach (var attr in type.GetCustomAttributes())
            {
                if (attr is SemanticEntityAttribute)
                {
                    var semantics = (SemanticEntityAttribute)attr;
                    res.Add(semantics.Prefix, semantics.EntityName);
                }
            }
            return res;
        }

        protected virtual Dictionary<string, List<SemanticProperty>> LoadPropertySemantics(Type type)
        {
            if (!EntityPropertySemantics.ContainsKey(type))
            {
                var result = new Dictionary<string, List<SemanticProperty>>();
                foreach (var pi in type.GetProperties())
                {
                    var name = pi.Name;
                    if (name != "Semantics")
                    {
                        bool defaultSet = false;
                        foreach (var attr in pi.GetCustomAttributes(true))
                        {
                            if (attr is SemanticPropertyAttribute)
                            {
                                if (!result.ContainsKey(name))
                                {
                                    result.Add(name, new List<SemanticProperty>());
                                }
                                var bits = ((SemanticPropertyAttribute)attr).PropertyName.Split(':');
                                if (bits.Length > 1)
                                {
                                    result[name].Add(new SemanticProperty(bits[0], bits[1]));
                                }
                                else
                                {
                                    result[name].Add(new SemanticProperty(null, bits[0]));
                                    defaultSet = true;
                                }
                            }
                        }
                        if (!defaultSet)
                        {
                            if (!result.ContainsKey(name))
                            {
                                result.Add(name, new List<SemanticProperty>());
                            }
                            //Add default semantics (the property name with the first character lower case)
                            result[name].Add(new SemanticProperty(name.Substring(0, 1).ToLower() + name.Substring(1)));
                        }
                    }
                
                }
                EntityPropertySemantics.Add(type, result);
            }
            return EntityPropertySemantics[type];
        }
    }
}
