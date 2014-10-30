using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace Sdl.Web.Mvc.Formats
{
    public interface IViewResultFormatter
    {
        bool IsSatisfiedBy(ControllerContext controllerContext);
        ActionResult CreateResult(ControllerContext controllerContext, ActionResult currentResult);
    }
    public static class ViewResultFormatters
    {
        public static Dictionary<string, IViewResultFormatter> Formatters { get; set; }
        static ViewResultFormatters()
        {
            Formatters = new Dictionary<string, IViewResultFormatter>();
            // add default formatters
            Formatters.Add("json", new JsonViewResultFormatter());
        }

        public static IViewResultFormatter GetFormatter(ControllerContext controllerContext)
        {
            string format = GetFormatParameter(controllerContext);
            if (format != null)
            {
                format = format.ToLower();
                if (Formatters.ContainsKey(format))
                {
                    return Formatters[format];
                }
            }
            foreach (var formatter in Formatters.Values)
            {
                if (formatter.IsSatisfiedBy(controllerContext))
                    return formatter;
            }
            return null;
        }

        private static string GetFormatParameter(ControllerContext controllerContext)
        {
            return controllerContext.RequestContext.HttpContext.Request.QueryString["format"];
        }
    }
}
