using System;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Mvc.ContentProvider;
using System.IO;

namespace Sdl.Web.Mvc.Controllers
{
    /// <summary>
    /// Base controller containing main controller actions (Page, PageRaw, Region, Entity, Navigation, List etc.)
    /// </summary>
    public abstract class BaseController : Controller
    {
        public virtual IContentProvider ContentProvider { get; set; }
        public virtual IRenderer Renderer { get; set; }
        public virtual ModelType ModelType { get; set; }

        public BaseController()
        {
            //default model type
            ModelType = ModelType.Entity;
        }

        /// <summary>
        /// Given a page URL, load the corresponding Page Model, Map it to the View Model and render it
        /// </summary>
        /// <param name="pageUrl">The page URL</param>
        /// <returns>Rendered Page View Model</returns>
        public virtual ActionResult Page(string pageUrl)
        {
            ModelType = ModelType.Page;
            var page = ContentProvider.GetPageModel(pageUrl);
            if (page == null)
            {
                return NotFound();
            }
            var viewData = GetViewData(page);
            SetupViewData(0, viewData);
            var model = ProcessModel(page, GetViewType(viewData)) ?? page;
            if (model is WebPage)
            {
                WebRequestContext.PageId = ((WebPage)model).Id;
            }
            return View(viewData.ViewName, model);
        }

        /// <summary>
        /// Given a page URL, load the corresponding raw content and write it to the response
        /// </summary>
        /// <param name="pageUrl">The page URL</param>
        /// <returns>raw page content</returns>
        public virtual ActionResult PageRaw(string pageUrl = null)
        {
            pageUrl = pageUrl ?? Request.Url.AbsolutePath;
            var rawContent = ContentProvider.GetPageContent(pageUrl);
            if (rawContent == null)
            {
                return NotFound();
            }
            return GetRawActionResult(Path.GetExtension(pageUrl).Substring(1), rawContent);
        }

        /// <summary>
        /// Render a file not found page
        /// </summary>
        /// <returns>404 page or HttpException if there is none</returns>
        public virtual ActionResult NotFound()
        {
            var page = ContentProvider.GetPageModel(WebRequestContext.Localization.Path + "/error-404");
            if (page == null)
            {
                throw new HttpException(404, "Page Not Found");
            }
            var viewData = GetViewData(page);
            SetupViewData(0, viewData);
            var model = ProcessModel(page, GetViewType(viewData)) ?? page;
            Response.StatusCode = 404;
            return View(viewData.ViewName, model);
        }
        
        /// <summary>
        /// Map and render a region model
        /// </summary>
        /// <param name="region">The region model</param>
        /// <param name="containerSize">The size (in grid units) of the container the region is in</param>
        /// <returns>Rendered region model</returns>
        [HandleSectionError(View = "SectionError")]
        public virtual ActionResult Region(IRegion region, int containerSize = 0)
        {
            ModelType = ModelType.Region;
            var viewData = GetViewData(region);
            SetupViewData(containerSize, viewData);
            var model = ProcessModel(region, GetViewType(viewData)) ?? region;
            return View(viewData.ViewName, model);
        }

        /// <summary>
        /// Map and render an entity model
        /// </summary>
        /// <param name="entity">The entity model</param>
        /// <param name="containerSize">The size (in grid units) of the container the entity is in</param>
        /// <returns>Rendered entity model</returns>
        [HandleSectionError(View = "SectionError")]
        public virtual ActionResult Entity(object entity, int containerSize = 0)
        {
            ModelType = ModelType.Entity;
            var viewData = GetViewData(entity);
            SetupViewData(containerSize, viewData);
            var model = ProcessModel(entity, GetViewType(viewData)) ?? entity;
            return View(viewData.ViewName, model);
        }

        /// <summary>
        /// Populate/Map and render a list entity model
        /// </summary>
        /// <param name="entity">The list entity model</param>
        /// <param name="containerSize">The size (in grid units) of the container the entity is in</param>
        /// <returns>Rendered list entity model</returns>
        [HandleSectionError(View = "SectionError")]
        public virtual ActionResult List(object entity, int containerSize = 0)
        {
            ModelType = ModelType.Entity;
            var viewData = GetViewData(entity);
            SetupViewData(containerSize, viewData);
            var model = ProcessList(entity, GetViewType(viewData)) ?? entity;
            return View(viewData.ViewName, model);
        }

        /// <summary>
        /// Populate and render a navigation entity model
        /// </summary>
        /// <param name="entity">The navigation entity</param>
        /// <param name="navType">The type of navigation to render</param>
        /// <param name="containerSize">The size (in grid units) of the container the navigation element is in</param>
        /// <returns></returns>
        [HandleSectionError(View = "SectionError")]
        public virtual ActionResult Navigation(object entity, string navType, int containerSize = 0)
        {
            ModelType = ModelType.Entity;
            var viewData = GetViewData(entity);
            SetupViewData(containerSize, viewData);
            var model = ProcessNavigation(entity, GetViewType(viewData), navType) ?? entity;
            return View(viewData.ViewName, model);
        }

        /// <summary>
        /// Populate and render an XML site map
        /// </summary>
        /// <param name="entity">The sitemap entity</param>
        /// <returns>Rendered XML sitemap</returns>
        public virtual ActionResult SiteMap(object entity=null)
        {
            var model = ContentProvider.GetNavigationModel(SiteConfiguration.LocalizeUrl("navigation.json", WebRequestContext.Localization));
            var viewData = GetViewData(entity);
            if (viewData.ViewName != null)
            {
                SetupViewData(0, viewData);
                return View(viewData.ViewName, model);
            }
            else
            {
                return View("SiteMapXml", model);
            }
        }

        /// <summary>
        /// Resolve a item ID into a url and redirect to that URL
        /// </summary>
        /// <param name="itemId">The item id to resolve</param>
        /// <param name="localization">The site localization in which to resolve the URL</param>
        /// <returns>null - response is redirected if the URL can be resolved</returns>
        public virtual ActionResult Resolve(string itemId, string localization)
        {
            var url = ContentProvider.ContentResolver.ResolveLink("tcm:" + itemId, localization);
            if (url == null)
            {
                var bits = itemId.Split(':');
                if (bits.Length > 1)
                {
                    bits = bits[1].Split('-');
                    foreach (var loc in SiteConfiguration.Localizations.Values)
                    {
                        if (loc.LocalizationId == bits[0])
                        {
                            url = loc.Path;
                        }
                    }
                }
            }
            if (url == null)
            {
                if (localization == null)
                {
                    url = SiteConfiguration.DefaultLocalization;
                }
                else
                {
                    var loc = SiteConfiguration.Localizations.Values.FirstOrDefault(l => l.LocalizationId.ToString(CultureInfo.InvariantCulture) == localization);
                    if (loc != null)
                    {
                        url = loc.Path;
                    }
                }
            }
            if (url != null)
            {
                Response.Redirect(url, true);
            }
            return null;
        }

        //This is the method to override if you need to add custom model population logic, first calling the base class and then adding your own logic
        protected virtual object ProcessModel(object sourceModel, Type type)
        {
            return ContentProvider.MapModel(sourceModel, ModelType, type);
        }

        protected virtual object ProcessList(object sourceModel, Type type)
        {
            var model = ProcessModel(sourceModel, type);
            var list = model as ContentList<Teaser>;
            if (list != null)
            {
                if (list.ItemListElements.Count == 0)
                {
                    //we need to run a query to populate the list
                    int start = GetRequestParameter<int>("start");
                    if (list.Id == Request.Params["id"])
                    {
                        //we only take the start from the query string if there is also an id parameter matching the model entity id
                        //this means that we are sure that the paging is coming from the right entity (if there is more than one paged list on the page)
                        list.CurrentPage = (start / list.PageSize) + 1;
                        list.Start = start;
                    }
                    ContentProvider.PopulateDynamicList(list);
                }
                model = list;
            }
            return model;
        }

        protected virtual object ProcessNavigation(object sourceModel, Type type, string navType)
        {
            var navigationUrl = SiteConfiguration.LocalizeUrl("navigation.json", WebRequestContext.Localization);
            var model = ProcessModel(sourceModel, type);
            var nav = model as NavigationLinks;
            NavigationLinks links = new NavigationLinks();
            switch (navType)
            {
                case "Top":
                    links = new NavigationBuilder { ContentProvider = ContentProvider, NavigationUrl = navigationUrl }.BuildTopNavigation(Request.Url.LocalPath);
                    break;
                case "Left":
                    links = new NavigationBuilder { ContentProvider = ContentProvider, NavigationUrl = navigationUrl }.BuildContextNavigation(Request.Url.LocalPath);
                    break;
                case "Breadcrumb":
                    links = new NavigationBuilder { ContentProvider = ContentProvider, NavigationUrl = navigationUrl }.BuildBreadcrumb(Request.Url.LocalPath);
                    break;
            }
            if (nav != null)
            {
                links.EntityData = nav.EntityData;
                links.PropertyData = nav.PropertyData;
            }
            return links;
        }
        
        protected virtual ActionResult GetRawActionResult(string type, string rawContent)
        {
            var contentType = type == "json" ? "application/json" : "text/" + type;
            return this.Content(rawContent, contentType);
        }

        protected virtual void SetupViewData(int containerSize = 0, MvcData viewData = null)
        {
            ViewBag.Renderer = Renderer;
            ViewBag.ContainerSize = containerSize;
            if (viewData != null)
            {
                ViewBag.RegionName = viewData.RegionName;
                //This enables us to jump areas when rendering sub-views - for example from rendering a region in Core to an entity in ModuleX
                ControllerContext.RouteData.DataTokens["area"] = viewData.AreaName;
            }
        }

        protected virtual Type GetViewType(MvcData viewData)
        {
            var key = String.Format("{0}:{1}", viewData.AreaName, viewData.ViewName);
            if (!SiteConfiguration.ViewModelRegistry.ContainsKey(key))
            {
                var viewEngineResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewData.ViewName);
                if (viewEngineResult.View == null)
                {
                    Log.Error("Could not find view {0} in locations: {1}", viewData.ViewName, String.Join(",", viewEngineResult.SearchedLocations));
                    throw new Exception(String.Format("Missing view: {0}", viewData.ViewName));
                }

                //This is the only way to get the view model type from the view and thus prevent the need to configure this somewhere
                var path = ((BuildManagerCompiledView)viewEngineResult.View).ViewPath;
                SiteConfiguration.AddViewModelToRegistry(viewData, path);
            }
            return SiteConfiguration.ViewModelRegistry[key];
        }

        protected virtual MvcData GetViewData(object sourceModel)
        {
            return ContentProvider.ContentResolver.ResolveMvcData(sourceModel);
        }

        protected virtual T GetRequestParameter<T>(string name)
        {
            var val = Request.Params[name];
            if (!String.IsNullOrEmpty(val))
            {
                try
                {
                    return (T)Convert.ChangeType(val,typeof(T));
                }
                catch (Exception)
                {
                    Log.Warn("Could not convert request parameter {0} into type {1}, using type default.", val, typeof(T));
                }
            }
            return default(T);
        }
    }
}
