using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Sdl.Web.Common;

namespace Sdl.Web.Mvc
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
            var result = filterContext.Result as ViewResult;
            var data = new ViewDataDictionary(new HandleErrorInfo(filterContext.Exception, (string)filterContext.RouteData.Values["controller"], (string)filterContext.RouteData.Values["action"]));
            filterContext.Result = new ViewResult { ViewName = this.View, ViewData = data };
            filterContext.ExceptionHandled = true;
            filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
        }
    }
}
