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
    /// Abstract Base Content Provider
    /// </summary>
    public abstract class BaseContentProvider : IContentProvider
    {
        public abstract string GetEntityViewName(object entity);
        public abstract string GetPageViewName(object entity);

        private static Dictionary<Type, IViewModelBuilder> _modelBuilders = null;
        public static Dictionary<Type, IViewModelBuilder> ModelBuilders
        {
            get
            {
                if (_modelBuilders == null)
                {
                    //TODO hardcoded and empty for now
                    _modelBuilders = new Dictionary<Type, IViewModelBuilder>();
                }
                return _modelBuilders;
            }
            set
            {
                _modelBuilders = value;
            }
        }
        public IViewModelBuilder DefaultModelBuilder { get; set; }

        public virtual object CreateEntityModel(object data, Type viewModeltype = null)
        {
            if (viewModeltype == null)
            {
                viewModeltype = GetEntityViewModelType(data);
            }
            if (viewModeltype!=null)
            {
                IViewModelBuilder builder = DefaultModelBuilder;
                if (ModelBuilders.ContainsKey(viewModeltype))
                {
                    builder = ModelBuilders[viewModeltype];
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

    }
}
