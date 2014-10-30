using System.Web.Mvc;
using System;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Configuration;
using System.Web.Routing;
using Sdl.Web.Mvc.Controllers;
using Sdl.Web.Common.Models.Common;

namespace Sdl.Web.Mvc.Formats
{
    /// <summary>
    /// Action Filter attritbute used to divert rendering from the standard View to a data formatter (for example JSON/RSS)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class FormatDataAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var formatter = DataFormatters.GetFormatter(filterContext);
            if (formatter != null)
            {
                var model = filterContext.Controller.ViewData.Model;
                if (formatter.ProcessModel)
                {
                    model = ProcessModel(model, filterContext);
                }
                var result = formatter.FormatData(filterContext, model);
                if (result != null)
                    filterContext.Result = result;
            }
            base.OnActionExecuted(filterContext);
        }

        private object ProcessModel(object model, ControllerContext context)
        {
            var pageData = model as WebPage;
            if (pageData!=null)
            {
                foreach(var region in pageData.Regions.Values)
                {
                    for (int i = 0; i < region.Items.Count; i++)
                    {
                        var entity = region.Items[i] as IEntity;
                        if (entity != null && entity.AppData!=null)
                        {
                            if (IsCustomAction(entity.AppData))
                            {
                                region.Items[i] = ProcessEntityModel(entity, context);
                            }
                        }
                    }
                }
            }
            return model;
        }

        private bool IsCustomAction(MvcData mvcData)
        {
            return mvcData.ActionName != SiteConfiguration.GetEntityAction() || mvcData.ControllerName != SiteConfiguration.GetEntityController() || mvcData.ControllerAreaName != SiteConfiguration.GetDefaultModuleName();
        }

        private object ProcessEntityModel(IEntity entity, ControllerContext context)
        {
            var tempRequestContext = new RequestContext(context.HttpContext, new RouteData());
            tempRequestContext.RouteData.DataTokens["Area"] = entity.AppData.ControllerAreaName;
            tempRequestContext.RouteData.Values["controller"] = entity.AppData.ControllerName;
            tempRequestContext.RouteData.Values["area"] = entity.AppData.ControllerAreaName;
            tempRequestContext.HttpContext = context.HttpContext;
           
            BaseController controller = ControllerBuilder.Current.GetControllerFactory().CreateController(tempRequestContext, entity.AppData.ControllerName) as BaseController;
            try
            {
                if (controller != null)
                {
                    controller.ControllerContext = new ControllerContext(context.HttpContext, tempRequestContext.RouteData, controller);
                    return controller.ProcessModel(entity);
                }
            }
            catch (Exception ex)
            {
                return new ExceptionEntity { Exception = ex };
            }
            return entity;
        }
    }
}
