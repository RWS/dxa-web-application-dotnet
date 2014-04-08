using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Sdl.Web.Mvc.Formatting
{
    /// <summary>
    /// Simplified from http://www.fatagnus.com/how-to-serve-the-same-data-in-json-xml-or-html-with-aspnet-mvc-revised/
    /// This is an experimental attribute to allow actions to be sent in different formats (only JSON currently)
    /// Note that this approach is probably too generic to be useful, as if we will probably want more dynamic JSON 
    /// (results of executed widgets, rather than widget content/metadata) in real life
    /// </summary> 
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class FormatFilterAttribute : FilterAttribute, IActionFilter
    {
        public static IContentNegotiator ContentNegotiator = new DefaultContentNegotiator();
        public static List<MediaTypeFormatter> Formatters = new List<MediaTypeFormatter> { new JsonMediaTypeFormatter(), new XmlMediaTypeFormatter() };
        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext.Result is ViewResult)
            {
                filterContext.Result = FormatViewResult(filterContext);
            }
        }

        private ActionResult FormatViewResult(ActionExecutedContext filterContext)
        {
            var view = (ViewResult)(filterContext.Result);
            var req = filterContext.HttpContext.Request;
            //Annoying: the content negotiator requires a HttpRequestMessage, which we have to manually create
            HttpRequestMessage reqMessage = new HttpRequestMessage();
            foreach (string headerName in req.Headers)
            {
                string[] headerValues = req.Headers.GetValues(headerName);
                if (!reqMessage.Headers.TryAddWithoutValidation(headerName, headerValues))
                {
                    reqMessage.Content.Headers.TryAddWithoutValidation(headerName, headerValues);
                }
            }
            var result = ContentNegotiator.Negotiate(view.ViewData.Model.GetType(), reqMessage, Formatters);
            if (result!=null && result.Formatter!=null)
            {
                if (result.Formatter is JsonMediaTypeFormatter)
                {
                    return new JsonResult { Data = view.ViewData.Model,JsonRequestBehavior=JsonRequestBehavior.AllowGet };
                }
            }
            return view;
        }


        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //Nothing to do
        }
    }
}
