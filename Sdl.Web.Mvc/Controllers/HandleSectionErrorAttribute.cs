using System.Web.Mvc;
using Sdl.Web.Common.Logging;

namespace Sdl.Web.Mvc.Controllers
{
    /// <summary>
    /// Handle error attribute for sub-sections of pages (entities/regions) which renders view for the error, but does not prevent the rest of the page being rendered
    /// </summary>
    public class HandleSectionErrorAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            base.OnException(filterContext);
            Log.Error(filterContext.Exception);
            ViewDataDictionary data = new ViewDataDictionary(new HandleErrorInfo(filterContext.Exception, (string)filterContext.RouteData.Values["controller"], (string)filterContext.RouteData.Values["action"]));
            filterContext.Result = new ViewResult { ViewName = View, ViewData = data };
            filterContext.ExceptionHandled = true;
            filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
        } 
    }
}
