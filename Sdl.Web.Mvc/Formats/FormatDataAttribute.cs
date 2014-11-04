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
    /// Action Filter attritbute used to divert rendering from the standard View to a 
    /// data formatter (for example JSON/RSS), and if necessary enriching the model
    /// by processing all entities to add external data
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class FormatDataAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var formatter = DataFormatters.GetFormatter(filterContext);
            if (formatter != null)
            {
                filterContext.Controller.ViewBag.DataFormatter = formatter;
                filterContext.Controller.ViewBag.AddIncludes = formatter.AddIncludes;
            }
            base.OnActionExecuting(filterContext);
        }
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var formatter = filterContext.Controller.ViewBag.DataFormatter as IDataFormatter;
            if (formatter != null)
            {
                var model = filterContext.Controller.ViewData.Model;
                if (formatter.ProcessModel)
                {
                    var controller = filterContext.Controller as BaseController;
                    if (controller!=null)
                    {
                        if (model is IPage)
                        {
                            model = controller.ProcessPageModel((IPage)model);
                        }
                        else
                        {
                            model = controller.ProcessEntityModel(model as IEntity) ?? model;
                        }
                    }
                }
                var result = formatter.FormatData(filterContext, model);
                if (result != null)
                {
                    filterContext.Result = result;
                }
            }
            base.OnActionExecuted(filterContext);
        }


    }
}
