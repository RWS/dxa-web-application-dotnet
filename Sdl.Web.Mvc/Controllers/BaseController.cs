using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Common;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Mvc.Formats;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Sdl.Web.Mvc.Html;

namespace Sdl.Web.Mvc.Controllers
{
    /// <summary>
    /// Base controller containing main controller actions (Page, Region, Entity, Navigation, List etc.)
    /// </summary>
    public abstract class BaseController : Controller
    {
#pragma warning disable 618
        private IRenderer _renderer = new BaseRenderer();

        [Obsolete("Renderers are deprecated in DXA 1.1. Rendering should be done using DXA 1.1 HtmlHelper extension methods.")]
        protected IRenderer Renderer
        {
            get
            {
                return _renderer;
            }
            set
            {
                // To support (deprecated) Dependency Injection
                _renderer = value;
            }
        }
#pragma warning restore 618

        protected IContentProvider ContentProvider
        {
            get; 
            set;
        }

        [Obsolete("Deprecated in DXA 1.1. The Model Type should be determined using the ViewModel class hierarchy.")]
        protected ModelType ModelType
        {
            get; 
            set;
        }

        /// <summary>
        /// Given a page URL, load the corresponding Page Model, Map it to the View Model and render it. 
        /// Can return XML or JSON if specifically requested on the URL query string (e.g. ?format=xml). 
        /// </summary>
        /// <param name="pageUrl">The page URL</param>
        /// <returns>Rendered Page View Model</returns>
        [FormatData]
        public virtual ActionResult Page(string pageUrl)
        {
#pragma warning disable 618
            ModelType = ModelType.Page;
#pragma warning restore 618
            bool addIncludes = ViewBag.AddIncludes ?? true; 
            PageModel page = ContentProvider.GetPageModel(pageUrl, addIncludes); 
            if (page == null)
            {
                return NotFound();
            }

            MvcData viewData = GetViewData(page);
            SetupViewData(0, viewData);
            PageModel model =  (EnrichModel(page) as PageModel) ?? page;

            if (!string.IsNullOrEmpty(model.Id))
            {
                WebRequestContext.PageId = model.Id;
            }

            return View(viewData.ViewName, model);
        }

        /// <summary>
        /// Render a file not found page
        /// </summary>
        /// <returns>404 page or HttpException if there is none</returns>
        [FormatData]
        public virtual ActionResult NotFound()
        {
            PageModel page = ContentProvider.GetPageModel(WebRequestContext.Localization.Path + "/error-404"); 
            if (page == null)
            {
                throw new HttpException(404, "Page Not Found");
            }
            MvcData viewData = GetViewData(page);
            SetupViewData(0, viewData);
            ViewModel model = (EnrichModel(page) as ViewModel) ?? page;
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
        public virtual ActionResult Region(RegionModel region, int containerSize = 0)
        {
#pragma warning disable 618
            ModelType = ModelType.Region;
#pragma warning restore 618
            SetupViewData(containerSize, region.MvcData);
            RegionModel model = (EnrichModel(region) as RegionModel) ?? region;
            return View(region.MvcData.ViewName, model);
        }

        /// <summary>
        /// Map and render an entity model
        /// </summary>
        /// <param name="entity">The entity model</param>
        /// <param name="containerSize">The size (in grid units) of the container the entity is in</param>
        /// <returns>Rendered entity model</returns>
        [HandleSectionError(View = "SectionError")]
        public virtual ActionResult Entity(EntityModel entity, int containerSize = 0)
        {
            SetupViewData(containerSize, entity.MvcData);
            EntityModel model = (EnrichModel(entity) as EntityModel) ?? entity;
            return View(entity.MvcData.ViewName, model);
        }

       

        /// <summary>
        /// Populate and render a navigation entity model
        /// </summary>
        /// <param name="entity">The navigation entity</param>
        /// <param name="navType">The type of navigation to render</param>
        /// <param name="containerSize">The size (in grid units) of the container the navigation element is in</param>
        /// <returns></returns>
        [HandleSectionError(View = "SectionError")]
        public virtual ActionResult Navigation(EntityModel entity, string navType, int containerSize = 0)
        {
            MvcData viewData = GetViewData(entity);
            SetupViewData(containerSize, viewData);
            EntityModel model = ProcessNavigation(entity, navType) ?? entity;
            return View(viewData.ViewName, model);
        }

        /// <summary>
        /// Retrieves a rendered HTML site map
        /// </summary>
        /// <param name="entity">The sitemap entity</param>
        /// <returns>Rendered site map HTML.</returns>
        public virtual ActionResult SiteMap(SitemapItem entity)
        {
            SitemapItem model = SiteConfiguration.NavigationProvider.GetNavigationModel(WebRequestContext.Localization);
            MvcData viewData = GetViewData(entity);
            SetupViewData(0, viewData);
            return View(viewData.ViewName, model);
        }

        /// <summary>
        /// Retrieves a Google XML site map
        /// </summary>
        /// <returns>Google site map XML.</returns>
        public virtual ActionResult SiteMapXml()
        {
            SitemapItem model = SiteConfiguration.NavigationProvider.GetNavigationModel(WebRequestContext.Localization);
            return View("SiteMapXml", model);
        }

        /// <summary>
        /// Retrieves a JSON site map
        /// </summary>
        /// <returns>Site map JSON.</returns>
        public virtual ActionResult SiteMapJson()
        {
            SitemapItem model = SiteConfiguration.NavigationProvider.GetNavigationModel(WebRequestContext.Localization);
            return Json(model, JsonRequestBehavior.AllowGet);
        }


        /// <summary>
        /// Resolve a item ID into a url and redirect to that URL
        /// </summary>
        /// <param name="itemId">The item id to resolve</param>
        /// <param name="localizationId">The site localization in which to resolve the URL</param>
        /// <param name="defaultItemId"></param>
        /// <param name="defaultPath"></param>
        /// <returns>null - response is redirected if the URL can be resolved</returns>
        public virtual ActionResult Resolve(string itemId, int localizationId, string defaultItemId = null, string defaultPath = null)
        {
            var url = SiteConfiguration.LinkResolver.ResolveLink("tcm:" + itemId, localizationId);
            if (url == null && defaultItemId!=null)
            {
                url = SiteConfiguration.LinkResolver.ResolveLink("tcm:" + defaultItemId, localizationId);
            }
            if (url == null)
            {
                url = String.IsNullOrEmpty(defaultPath) ? "/" : defaultPath;
            }
            return Redirect(url);
        }

        /// <summary>
        /// This is the method to override if you need to add custom model population logic, 
        /// first calling the base class and then adding your own logic
        /// </summary>
        /// <param name="model">The model which you wish to add data to</param>
        /// <returns>A fully populated view model combining CMS content with other data</returns>
        public virtual ViewModel EnrichModel(ViewModel model)
        {
            //Check if an exception was generated when creating the model, so now is the time to throw it
            if (model is ExceptionEntity)
            {
                throw new Exception(((ExceptionEntity)model).Error);
            }
#pragma warning disable 618
            return (ViewModel) ProcessModel(model, model.GetType()); // To support legacy overrides
#pragma warning restore 618
        }

        
        /// <summary>
        /// This is the method to override if you need to add custom model population logic, first calling the base class and then adding your own logic
        /// </summary>
        /// <param name="sourceModel">The model to process</param>
        /// <param name="type">The type of view model required</param>
        /// <returns>A processed view model</returns>
        [Obsolete("Deprecated in DXA 1.1. Override EnrichModel instead.")]
        protected virtual object ProcessModel(object sourceModel, Type type)
        {
            // NOTE: Intentionally loosely typed for backwards compatibility; this was part of the V1.0 (semi-)public API

            return sourceModel;
        }

        protected virtual EntityModel ProcessNavigation(EntityModel sourceModel, string navType)
        {
            INavigationProvider navigationProvider = SiteConfiguration.NavigationProvider;
            string requestUrlPath = Request.Url.LocalPath;
            Localization localization = WebRequestContext.Localization;
            NavigationLinks navigationLinks;
            switch (navType)
            {
                case "Top":
                    navigationLinks = navigationProvider.GetTopNavigationLinks(requestUrlPath, localization);
                    break;
                case "Left":
                    navigationLinks = navigationProvider.GetContextNavigationLinks(requestUrlPath, localization);
                    break;
                case "Breadcrumb":
                    navigationLinks = navigationProvider.GetBreadcrumbNavigationLinks(requestUrlPath, localization);
                    break;
                default:
                    throw new DxaException("Unexpected navType: " + navType);
            }

            NavigationLinks navModel = EnrichModel(sourceModel) as NavigationLinks;
            if (navModel != null)
            {
                navigationLinks.XpmMetadata = navModel.XpmMetadata;
                navigationLinks.XpmPropertyMetadata = navModel.XpmPropertyMetadata;
            }
            return navigationLinks;
        }
        
        protected virtual ActionResult GetRawActionResult(string type, string rawContent)
        {
            string contentType;
            switch (type)
            {
                case "json":
                    contentType = "application/json";
                    break;
                case "xml":
                case "rss":
                case "atom":
                    contentType = type.Equals("xml") ? "text/xml" : String.Format("application/{0}+xml", type);
                    break;
                default:
                    contentType = "text/" + type;
                    break;
            }
            return Content(rawContent, contentType);
        }

        protected virtual void SetupViewData(int containerSize = 0, MvcData viewData = null)
        {
#pragma warning disable 618
            // To support (deprecated) use of ViewBag.Renderer in Views.
            ViewBag.Renderer = Renderer;
#pragma warning restore 618

            ViewBag.ContainerSize = containerSize;
            if (viewData != null)
            {
                ViewBag.RegionName = viewData.RegionName;
                //This enables us to jump areas when rendering sub-views - for example from rendering a region in Core to an entity in ModuleX
                ControllerContext.RouteData.DataTokens["area"] = viewData.AreaName;
            }
        }

        [Obsolete("Method is deprecated in DXA 1.1.")]
        protected virtual Type GetViewType(MvcData viewData)
        {
            return ModelTypeRegistry.GetViewModelType(viewData);
        }

        protected virtual MvcData GetViewData(ViewModel viewModel)
        {
            return viewModel == null ? null : viewModel.MvcData;
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

        public virtual object ProcessPageModel(PageModel model)
        {
            // For each entity in the page which has a custom controller action (so is likely
            // to enrich the CMS managed model with additional data) we call the 
            // controller ProcessModel method, and update our model with the enriched
            // data
            if (model != null)
            {
                foreach (RegionModel region in model.Regions)
                {
                    for (int i = 0; i < region.Entities.Count; i++)
                    {
                        EntityModel entity = region.Entities[i];
                        if (entity != null && entity.MvcData != null)
                        {
                            region.Entities[i] = ProcessEntityModel(entity);                            
                        }
                    }
                }
            }
            return model;
        }

        public virtual EntityModel ProcessEntityModel(EntityModel entity)
        {
            //Enrich a base (CMS managed) entity with additional data by calling the
            //appropriate custom controller's ProcessModel method
            if (entity!=null && IsCustomAction(entity.MvcData))
            {
                MvcData mvcData = entity.MvcData;

                var tempRequestContext = new RequestContext(HttpContext, new RouteData());
                tempRequestContext.RouteData.DataTokens["Area"] = mvcData.ControllerAreaName;
                tempRequestContext.RouteData.Values["controller"] = mvcData.ControllerName;
                tempRequestContext.RouteData.Values["area"] = mvcData.ControllerAreaName;
                tempRequestContext.HttpContext = HttpContext;
                BaseController controller = ControllerBuilder.Current.GetControllerFactory().CreateController(tempRequestContext, mvcData.ControllerName) as BaseController;
                try
                {
                    if (controller != null)
                    {
                        controller.ControllerContext = new ControllerContext(HttpContext, tempRequestContext.RouteData, controller);
                        return (EntityModel) controller.EnrichModel(entity);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    return new ExceptionEntity { Error = ex.Message };
                }
            }
            return entity;
        }

        protected virtual bool IsCustomAction(MvcData mvcData)
        {
            return mvcData.ActionName != SiteConfiguration.GetEntityAction() || mvcData.ControllerName != SiteConfiguration.GetEntityController() || mvcData.ControllerAreaName != SiteConfiguration.GetDefaultModuleName();
        }

    }
}
