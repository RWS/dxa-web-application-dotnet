using System;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Mvc.Controllers
{
    /// <summary>
    /// Abstract base class for DXA Controllers 
    /// </summary>
    public abstract class BaseController : Controller
    {
        private IContentProvider _contentProvider;

        /// <summary>
        /// Gets or sets the Content Provider.
        /// </summary>
        /// <remarks>
        /// Setting this property is no longer needed, but setter is kept for backwards compatibility.
        /// </remarks>
        protected IContentProvider ContentProvider
        {
            get { return _contentProvider ?? (_contentProvider = SiteConfiguration.ContentProvider); }
            set
            {
                // To support (deprecated) Dependency Injection
                _contentProvider = value;
            }
        }

        /// <summary>
        /// Enriches the View Model as obtained from the Content Provider.
        /// </summary>
        /// <param name="model">The View Model to enrich.</param>
        /// <returns>The enriched View Model.</returns>
        /// <remarks>
        /// This is the method to override if you need to add custom model population logic. 
        /// For example retrieving additional information from another system.
        /// </remarks>
        protected virtual ViewModel EnrichModel(ViewModel model)
            => model;

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
            ViewData[DxaViewDataItems.ContainerSize] = containerSize;
            if (viewData != null)
            {
                ViewData[DxaViewDataItems.RegionName] = viewData.RegionName;
                //This enables us to jump areas when rendering sub-views - for example from rendering a region in Core to an entity in ModuleX
                ControllerContext.RouteData.DataTokens["area"] = viewData.AreaName;
            }
        }

        protected virtual void SetupViewData(ViewModel viewModel, int containerSize = 0)
        {
            SetupViewData(containerSize, viewModel.MvcData);
        }

        /// <summary>
        /// Gets the typed value of a request parameter (from the URL query string) with a given name.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="name">The name of the parameter.</param>
        /// <returns>The typed value of the request parameter or the default value for the given type if the parameter is not specified or cannot be converted to the type.</returns>
        protected virtual T GetRequestParameter<T>(string name)
        {
            T value;
            TryGetRequestParameter(name, out value);
            return value;
        }

        /// <summary>
        /// Tries to get the typed value of a request parameter (from the URL query string) with a given name.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The typed value of the parameter (output).</param>
        /// <returns><c>true</c> if the parameter is specified and its value can be converted to the given type; <c>false</c> otherwise.</returns>
        protected bool TryGetRequestParameter<T>(string name, out T value)
        {
            string paramValue = Request.Params[name];
            if (string.IsNullOrEmpty(paramValue))
            {
                Log.Debug("Request parameter '{0}' is not specified.", name);
                value = default(T);
                return false;
            }

            try
            {
                value = (T) Convert.ChangeType(paramValue, typeof(T));
                return true;
            }
            catch (Exception)
            {
                Log.Warn("Could not convert value for request parameter '{0}' into type {1}. Value: '{2}'.", name, typeof(T).Name, paramValue);
                value = default(T);
                return false;
            }
        }

        /// <summary>
        /// Enriches a given Entity Model using an appropriate (custom) Controller.
        /// </summary>
        /// <param name="entity">The Entity Model to enrich.</param>
        /// <returns>The enriched Entity Model.</returns>
        /// <remarks>
        /// This method is different from <see cref="EnrichModel"/> in that it doesn't expect the current Controller to be able to enrich the Entity Model;
        /// it creates a Controller associated with the Entity Model for that purpose.
        /// It is used by <see cref="PageController.EnrichEmbeddedModels"/>.
        /// </remarks>
        protected EntityModel EnrichEntityModel(EntityModel entity)
        {
            if (entity == null || entity.MvcData == null || !IsCustomAction(entity.MvcData))
            {
                return entity;
            }

            MvcData mvcData = entity.MvcData;
            using (new Tracer(entity, mvcData))
            {
                string controllerName = mvcData.ControllerName ?? SiteConfiguration.GetEntityController();
                string controllerAreaName = mvcData.ControllerAreaName ?? SiteConfiguration.GetDefaultModuleName();

                RequestContext tempRequestContext = new RequestContext(HttpContext, new RouteData());
                tempRequestContext.RouteData.DataTokens["Area"] = controllerAreaName;
                tempRequestContext.RouteData.Values["controller"] = controllerName;
                tempRequestContext.RouteData.Values["area"] = controllerAreaName;

                // Note: Entity Controllers don't have to inherit from EntityController per se, but they must inherit from BaseController.
                BaseController entityController = (BaseController) ControllerBuilder.Current.GetControllerFactory().CreateController(tempRequestContext, controllerName);
                entityController.ControllerContext = new ControllerContext(HttpContext, tempRequestContext.RouteData, entityController);
                return (EntityModel) entityController.EnrichModel(entity);
            }
        }

        private static bool IsCustomAction(MvcData mvcData)
        {
            return mvcData.ActionName != SiteConfiguration.GetEntityAction()
                || mvcData.ControllerName != SiteConfiguration.GetEntityController()
                || mvcData.ControllerAreaName != SiteConfiguration.GetDefaultModuleName();
        }

        /// <summary>
        /// Creates a JSON Result which uses the JSON.NET serializer.
        /// </summary>
        /// <remarks>
        /// By default, ASP.NET MVC uses the JavaScriptSerializer to serialize objects to JSON.
        /// By overriding this method, we ensure that the (more powerful and faster) JSON.NET serializer is used when <see cref="BaseController"/>-derived
        /// controller uses the standard ASP.NET MVC Json method to return a JSON result.
        /// </remarks>.
        protected override JsonResult Json(object data, string contentType, Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            return new Sdl.Web.Mvc.Formats.JsonNetResult
            {
                Data = data,
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                JsonRequestBehavior = behavior
            };
        }
    }
}
