using System;
using System.Web.Mvc;
using System.Web.Routing;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Common;
using Sdl.Web.Mvc.Html;

namespace Sdl.Web.Mvc.Controllers
{
    /// <summary>
    /// Abstract base class for DXA Controllers 
    /// </summary>
    public abstract class BaseController : Controller
    {
        private IContentProvider _contentProvider;
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

        /// <summary>
        /// Gets or sets the Content Provider.
        /// </summary>
        /// <remarks>
        /// Setting this property is no longer needed, but setter is kept for backwards compatibility.
        /// </remarks>
        protected IContentProvider ContentProvider
        {
            get
            {
                if (_contentProvider == null)
                {
                    _contentProvider = SiteConfiguration.ContentProvider;
                }
                return _contentProvider;
            }
            set
            {
                // To support (deprecated) Dependency Injection
                _contentProvider = value;
            }
        }

        [Obsolete("Deprecated in DXA 1.1. The Model Type should be determined using the ViewModel class hierarchy.")]
        protected ModelType ModelType
        {
            get; 
            set;
        }

        /// <summary>
        /// This is the method to override if you need to add custom model population logic, 
        /// first calling the base class and then adding your own logic
        /// </summary>
        /// <param name="model">The model which you wish to add data to</param>
        /// <returns>A fully populated view model combining CMS content with other data</returns>
        protected virtual ViewModel EnrichModel(ViewModel model)
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

        protected virtual void SetupViewData(ViewModel viewModel, int containerSize = 0)
        {
#pragma warning disable 618
            // Set the (deprecated) ModelType property based on the View Model type.
            if (viewModel is PageModel)
            {
                ModelType = ModelType.Page;
            }
            else if (viewModel is RegionModel)
            {
                ModelType = ModelType.Region;
            }
#pragma warning restore 618

            SetupViewData(containerSize, viewModel.MvcData);
        }

        [Obsolete("Deprecated in DXA 1.1.")]
        protected virtual Type GetViewType(MvcData viewData)
        {
            return ModelTypeRegistry.GetViewModelType(viewData);
        }

        [Obsolete("Deprecated in DXA 1.1. Use ViewModel.MvcData directly.")]
        protected virtual MvcData GetViewData(ViewModel viewModel)
        {
            return viewModel == null ? null : viewModel.MvcData;
        }

        protected virtual T GetRequestParameter<T>(string name)
        {
            string val = Request.Params[name];
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

                RequestContext tempRequestContext = new RequestContext(HttpContext, new RouteData());
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
            return mvcData.ActionName != SiteConfiguration.GetEntityAction() 
                || mvcData.ControllerName != SiteConfiguration.GetEntityController() 
                || mvcData.ControllerAreaName != SiteConfiguration.GetDefaultModuleName();
        }

    }
}
