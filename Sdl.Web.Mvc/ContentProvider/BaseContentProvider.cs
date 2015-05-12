using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Script.Serialization;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Mvc.ContentProvider
{
    /// <summary>
    /// Abstract Base Content Provider
    /// </summary>
    public abstract class BaseContentProvider : IContentProvider
    {
        public IContentResolver ContentResolver { get; set; }

        //These need to be implemented by the specific content provider
        public abstract string GetPageContent(string url);
        public abstract EntityModel GetEntityModel(string id);
        public abstract string GetEntityContent(string url);
        public abstract ContentList<Teaser> PopulateDynamicList(ContentList<Teaser> list);

        protected abstract object GetPageModelFromUrl(string url); // TODO TSI-634: strongly typed.
        protected abstract List<PageModel> GetIncludesFromModel(object data); // TODO TSI-634: strongly typed.

        private static Dictionary<Type, IModelBuilder> _modelBuilders;
        /// <summary>
        /// Type to IModelBuilder mapping used when determining how to map models
        /// </summary>
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

        /// <summary>
        /// Default Model Builder, used to map models for whose type a specific model builder has not been specified
        /// </summary>
        public IModelBuilder DefaultModelBuilder { get; set; }

        /// <summary>
        /// Get the model for a page given the URL
        /// </summary>
        /// <param name="url">Page URL</param>
        /// <param name="addIncludes">If true then includes will be added in the model</param>
        /// <returns>Model corresponding to that URL</returns>
        public PageModel GetPageModel(string url, bool addIncludes)
        {
            var parsedUrl = ParseUrl(url);
            Log.Debug("Getting page model for URL {0} (original request: {1})", parsedUrl, url);
            //We can have a couple of tries to get the page model if there is no file extension on the url request, but it does not end in a slash:
            //1. Try adding the default extension, so /news becomes /news.html
            var model = GetPageModelFromUrl(parsedUrl);
            if (model == null && (url==null || (!url.EndsWith("/") && url.LastIndexOf(".", StringComparison.Ordinal) <= url.LastIndexOf("/", StringComparison.Ordinal))))
            {
                //2. Try adding the default page, so /news becomes /news/index.html
                parsedUrl = ParseUrl(url + "/");
                Log.Debug("No content for URL found, trying default: {0}", parsedUrl);
                model = GetPageModelFromUrl(parsedUrl);
            }
            return (PageModel) MapModel(model, addIncludes: addIncludes);
        }
        
        /// <summary>
        /// Map the domain (CMS) model to the presentation (View) Model
        /// </summary>
        /// <param name="data">The domain model</param>
        /// <param name="viewModeltype">The presentation model Type to map to</param>
        /// <returns></returns>
        public virtual ViewModel MapModel(object data, Type viewModeltype = null, bool addIncludes = true)
        {
            if (data == null)
            {
                return null;
            }

            MvcData viewData = ContentResolver.ResolveMvcData(data);
            if (viewModeltype == null)
            {
                viewModeltype = ModelTypeRegistry.GetViewModelType(viewData);
            }
            if (data.GetType() == viewModeltype)
            {
                //model already mapped to required type
                return (ViewModel) data;
            }
            List<PageModel> includes = addIncludes ? GetIncludesFromModel(data) : new List<PageModel>();
            IModelBuilder builder = DefaultModelBuilder;
            if (ModelBuilders.ContainsKey(viewModeltype))
            {
                builder = ModelBuilders[viewModeltype];
            }
            return builder.Create(data, viewModeltype, includes, viewData);
        }

        /// <summary>
        /// Get the model for a navigation structure
        /// </summary>
        /// <param name="url">The URL representing the navigation structure</param>
        /// <returns>A navigation model for the given URL</returns>
        public virtual SitemapItem GetNavigationModel(string url)
        {
            string key = "navigation-" + url;
            //This is a temporary measure to cache the navigationModel per request to not retrieve and serialize 3 times per request. Comprehensive caching strategy pending
            SitemapItem result;
            if (HttpContext.Current.Items[key] == null)
            {
                string navigationJsonString = GetPageContent(url);
                result = new JavaScriptSerializer().Deserialize<SitemapItem>(navigationJsonString);
                HttpContext.Current.Items[key] = result;
            }
            else
            {
                result = (SitemapItem) HttpContext.Current.Items[key];
            }
            return result;
        }

        /// <summary>
        /// Converts a request URL into a CMS URL (for example adding default page name, and file extension)
        /// </summary>
        /// <param name="url">The request URL</param>
        /// <returns>A CMS URL</returns>
        public virtual string ParseUrl(string url)
        {
            var defaultPageFileName = ContentResolver.DefaultPageName;
            if (String.IsNullOrEmpty(url))
            {
                url = defaultPageFileName;
            }
            if (url.EndsWith("/"))
            {
                url = url + defaultPageFileName;
            }
            if (!Path.HasExtension(url))
            {
                url = url + ContentResolver.DefaultExtension;
            }
            return url;
        }
    }
}
