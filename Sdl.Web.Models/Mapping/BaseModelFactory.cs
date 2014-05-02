using Sdl.Web.Mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc.Mapping
{
    /// <summary>
    /// Default EntityMapper - simply returns the same entity
    /// </summary>
    public class BaseModelFactory : IModelFactory
    {
        public virtual object CreateEntityModel(object data, string view)
        {
            //in an ideal world, we do not need to map...
            return data;
        }

        public virtual object CreatePageModel(object data, string view = null, Dictionary<string,object> subPages = null)
        {
            //in an ideal world, we do not need to map...
            return data;
        }

        protected virtual object GetEntity(string entityType)
        {
            var semantics = new Semantics { Type = entityType, Vocabulary = GetDefaultVocabulary() };
            entityType = "Sdl.Web.Mvc.Models." + entityType;
            //TODO models will not always be in this assembly (modules, third party/custom etc.)
            try
            {
                var entity = Activator.CreateInstance("Sdl.Web.Mvc", entityType).Unwrap();
                PropertyInfo pi = entity.GetType().GetProperty("Semantics");
                if (pi != null)
                {
                    pi.SetValue(entity, semantics);
                }
                return entity;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not load entity type {0}", entityType);
            }
            return null;
        }

        protected virtual string GetDefaultVocabulary()
        {
            return "http://schema.org";
        }
    }
}
