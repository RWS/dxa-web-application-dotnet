using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Controllers;
using System;
using System.Web.Mvc;

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
            IDataFormatter formatter = DataFormatters.GetFormatter(filterContext);
            if (formatter != null)
            {
                filterContext.Controller.ViewBag.DataFormatter = formatter;
                filterContext.Controller.ViewBag.AddIncludes = formatter.AddIncludes;
            }
            base.OnActionExecuting(filterContext);
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            IDataFormatter formatter = filterContext.Controller.ViewBag.DataFormatter as IDataFormatter;
            if (formatter != null)
            {
                object model = filterContext.Controller.ViewData.Model;
                if (formatter.ProcessModel)
                {
                    BaseController controller = filterContext.Controller as BaseController;
                    if (controller!=null)
                    {
                        if (model is PageModel)
                        {
                            model = controller.ProcessPageModel((PageModel)model);
                        }
                        else
                        {
                            model = controller.ProcessEntityModel(model as EntityModel) ?? model;
                        }
                    }
                }
                ActionResult result = formatter.FormatData(filterContext, model);
                if (result != null)
                {
                    filterContext.Result = result;
                }
            }
            base.OnActionExecuted(filterContext);
        }
    }
}
