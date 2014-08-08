using System.Web.Mvc;

namespace Sdl.Web
{
    public partial class ErrorController : Controller
    {
        [PreventDirectAccess]
        public ActionResult ServerError()
        {
            Response.StatusCode = 500; 
            return View();
        }

        [PreventDirectAccess]
        public ActionResult NotFound()
        {
            Response.StatusCode = 404; 
            return View();
        }

        [PreventDirectAccess]
        public ActionResult OtherHttpStatusCode(int httpStatusCode)
        {
            return View("GenericHttpError", httpStatusCode);
        }

        [Route("se_blank")]
        public ActionResult Blank()
        {
            return Content("<!-- se_blank.html -->");
        }

        private class PreventDirectAccessAttribute : FilterAttribute, IAuthorizationFilter
        {
            public void OnAuthorization(AuthorizationContext filterContext)
            {
                object value = filterContext.RouteData.Values["fromAppErrorEvent"];
                if (!(value is bool && (bool)value))
                    filterContext.Result = new ViewResult { ViewName = "Error404" };
            }
        }
    }
}