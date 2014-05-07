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
    public abstract class BaseModelFactory : IModelFactory
    {
        public abstract string GetEntityViewName(object entity);
        public abstract string GetPageViewName(object entity);

        private static Dictionary<Type, IEntityBuilder> _entityBuilders = null;
        public static Dictionary<Type, IEntityBuilder> EntityBuilders
        {
            get
            {
                if (_entityBuilders == null)
                {
                    //TODO hardcoded and empty for now
                    _entityBuilders = new Dictionary<Type, IEntityBuilder>();
                }
                return _entityBuilders;
            }
            set
            {
                _entityBuilders = value;
            }
        }
        public IEntityBuilder DefaultEntityBuilder { get; set; }

        public virtual object CreateEntityModel(object data, Type viewModeltype = null)
        {
            if (viewModeltype == null)
            {
                viewModeltype = GetEntityViewModelType(data);
            }
            if (viewModeltype!=null)
            {
                IEntityBuilder builder = DefaultEntityBuilder;
                if (EntityBuilders.ContainsKey(viewModeltype))
                {
                    builder = EntityBuilders[viewModeltype];
                }
                return builder.Create(data, viewModeltype);
            }
            else
            {
                var ex = new Exception(String.Format("Cannot find view model for entity in ViewModelRegistry. Check the view is strongly typed using the @model statement"));
                Log.Error(ex);
                throw ex;
            }
        }

        public virtual Type GetEntityViewModelType(object data)
        {
            var viewName = GetEntityViewName(data);
            return Configuration.ViewModelRegistry.ContainsKey(viewName) ? Configuration.ViewModelRegistry[viewName] : null;
        }

        public virtual object CreatePageModel(object data, Dictionary<string, object> subPages = null, string viewName = null)
        {
            //in an ideal world, we do not need to map...
            return data;
        }

        /*protected virtual object GetEntity(string entityType)
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
        }*/

    }
}
