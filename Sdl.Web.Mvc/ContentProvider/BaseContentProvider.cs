using Sdl.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;


namespace Sdl.Web.Mvc
{
    /// <summary>
    /// Abstract Base Content Provider
    /// </summary>
    public abstract class BaseContentProvider : IContentProvider
    {
        public BaseContentProvider()
        {
            DefaultExtension = ".html";
            DefaultExtensionLessPageName = Configuration.GetDefaultDocument();
            DefaultPageName = DefaultExtensionLessPageName + DefaultExtension;
        }

        //These need to be implemented by the specific content provider
        public abstract string GetPageContent(string url);
        public abstract object GetEntityModel(string id);
        public abstract string GetEntityContent(string url);
        public abstract ViewData GetEntityViewData(object entity);
        public abstract ViewData GetPageViewData(object page);
        public abstract ViewData GetRegionViewData(object region);

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
            var defaultPageFileName = DefaultPageName;
            return String.IsNullOrEmpty(url) ? defaultPageFileName : (url.EndsWith("/") ? url + defaultPageFileName : url += DefaultExtension);
        }
        
        public virtual object MapModel(object data, ModelType modelType, Type viewModeltype = null)
        {
            List<object> includes = GetIncludesFromModel(data, modelType);
            ViewData viewData = null;
            switch (modelType)
            {
                case ModelType.Page:
                    viewData = GetPageViewData(data);
                    break;
                case ModelType.Region:
                    viewData = GetRegionViewData(data);
                    break;
                default:
                    viewData = GetEntityViewData(data);
                    break;
            }
            if (viewModeltype == null)
            {
                var key = String.Format("{0}:{1}", viewData.AreaName, viewData.ViewName);
                viewModeltype = Configuration.ViewModelRegistry.ContainsKey(key) ? Configuration.ViewModelRegistry[key] : null;
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

        protected abstract List<object> GetIncludesFromModel(object data, ModelType modelType);

        /// <summary>
        /// Used to post process URLs - for example to remove extensions and default document from resolved links, so for example /news/index.html becomes /news/
        /// </summary>
        /// <param name="url">The URL to process</param>
        /// <returns>The processed URL</returns>
        public virtual string ProcessUrl(string url, string localizationId = null)
        {
            if (url != null)
            {
                if (url.EndsWith(DefaultExtension))
                {
                    url = url.Substring(0, url.Length - DefaultExtension.Length);
                    if (url.EndsWith("/" + DefaultExtensionLessPageName))
                    {
                        url = url.Substring(0, url.Length - DefaultExtensionLessPageName.Length);
                    }
                }
            }
            return url;
        }

        public abstract void PopulateDynamicList(ContentList<Teaser> list);

        protected static string DefaultExtensionLessPageName{get;set;}
        protected static string DefaultPageName{get;set;}
        protected static string DefaultExtension{get;set;}



        public virtual object GetNavigationModel(string url)
        {
            string key = "navigation-" + url;
            //This is a temporary measure to cache the navigationModel per request to not retrieve and serialize 3 times per request. Comprehensive caching strategy pending
            if (HttpContext.Current.Items[key] == null)
            {
                string navigationJsonString = GetPageContent(url);
                var navigationModel = new JavaScriptSerializer().Deserialize<SitemapItem>(navigationJsonString);
                HttpContext.Current.Items[key] = navigationModel;
            }
            return HttpContext.Current.Items[key] as SitemapItem;
        }
    }
}
