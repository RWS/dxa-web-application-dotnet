using Sdl.Web.Mvc.Common;
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
        //These need to be implemented by the specific content provider
        public abstract string GetPageContent(string url);
        public abstract object GetEntityModel(string id);
        public abstract string GetEntityContent(string url);
        public abstract string GetEntityViewName(object entity);
        public abstract string GetPageViewName(object page);
        public abstract string GetRegionViewName(object region);

        protected abstract object GetPageModelFromUrl(string url);
        
        public object GetPageModel(string url)
        {
            //We can have a couple of tries to get the page model if there is no file extension on the url request, but it does not end in a slash:
            //1. Try adding the default extension, so /news becomes /news.html
            var model = GetPageModelFromUrl(ParseUrl(url));
            if (model == null && !url.EndsWith("/") && url.LastIndexOf(".", StringComparison.Ordinal) <= url.LastIndexOf("/", StringComparison.Ordinal))
            {
                //2. Try adding the default page, so /news becomes /news/index.html
                model = GetPageModelFromUrl(ParseUrl(url + "/"));
            }
            return model;
        }

        private static Dictionary<Type, IModelBuilder> _modelBuilders = null;
        public static Dictionary<Type, IModelBuilder> ModelBuilders
        {
            get
            {
                if (_modelBuilders == null)
                {
                    //TODO hardcoded and empty for now
                    _modelBuilders = new Dictionary<Type, IModelBuilder>();
                }
                return _modelBuilders;
            }
            set
            {
                _modelBuilders = value;
            }
        }
        public IModelBuilder DefaultModelBuilder { get; set; }
        
        public virtual string ParseUrl(string url)
        {
            var defaultPageFileName = Configuration.GetDefaultPageName();
            return String.IsNullOrEmpty(url) ? defaultPageFileName : (url.EndsWith("/") ? url + defaultPageFileName : url += Configuration.GetDefaultExtension());
        }
        
        public virtual object MapModel(object data, ModelType modelType, Type viewModeltype = null, List<object> includes = null)
        {
            string viewName = null;
            switch (modelType)
            {
                case ModelType.Page:
                    viewName = GetPageViewName(data);
                    break;
                case ModelType.Region:
                    viewName = GetRegionViewName(data);
                    break;
                default:
                    viewName = GetEntityViewName(data);
                    break;
            }
            if (viewModeltype == null)
            {
                viewModeltype = Configuration.ViewModelRegistry.ContainsKey(viewName) ? Configuration.ViewModelRegistry[viewName] : null;
            }
            if (viewModeltype!=null)
            {
                IModelBuilder builder = DefaultModelBuilder;
                if (ModelBuilders.ContainsKey(viewModeltype))
                {
                    builder = ModelBuilders[viewModeltype];
                }
                return builder.Create(data, viewModeltype, includes);
            }
            else
            {
                var ex = new Exception(String.Format("Cannot find view model for entity in ViewModelRegistry. Check the view is strongly typed using the @model statement"));
                Log.Error(ex);
                throw ex;
            }
        }

        public virtual string ProcessUrl(string url)
        {
            if (url != null)
            {
                if (url.EndsWith(Configuration.GetDefaultExtension()))
                {
                    url = url.Substring(0, url.Length - Configuration.GetDefaultExtension().Length);
                    if (url.EndsWith("/" + Configuration.GetDefaultExtensionLessPageName()))
                    {
                        url = url.Substring(0, url.Length - Configuration.GetDefaultExtensionLessPageName().Length);
                    }
                }
            }
            return url;
        }
    }
}
