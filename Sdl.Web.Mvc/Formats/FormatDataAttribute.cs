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
                filterContext.Controller.ViewData[DxaViewDataItems.DisableOutputCache] = true;
                filterContext.Controller.ViewData[DxaViewDataItems.DataFormatter] = formatter;
                filterContext.Controller.ViewData[DxaViewDataItems.AddIncludes] = formatter.AddIncludes;
            }
            else
            {
                filterContext.Controller.ViewData[DxaViewDataItems.DisableOutputCache] = false;
            }
            base.OnActionExecuting(filterContext);
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            ControllerBase controller = filterContext.Controller;
            IDataFormatter formatter = controller.ViewData[DxaViewDataItems.DataFormatter] as IDataFormatter;

            // Once we got here, we expect the View Model to be enriched already, but in case of a Page Model,
            // the embedded Region/Entity Models won't be enriched yet.
            if (formatter != null && formatter.ProcessModel && controller is PageController)
            {
                PageModel pageModel = controller.ViewData.Model as PageModel;
                ((PageController) controller).EnrichEmbeddedModels(pageModel);
                ActionResult result = formatter.FormatData(filterContext, pageModel);
                if (result != null)
                {
                    filterContext.Result = result;
                }
            }
            base.OnActionExecuted(filterContext);
        }
    }
}
