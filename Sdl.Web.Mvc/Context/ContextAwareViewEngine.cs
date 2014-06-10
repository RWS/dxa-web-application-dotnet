using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Sdl.Web.Mvc
{
    public class ContextAwareViewEngine : RazorViewEngine
    {
        //For future addition of context-based view selection
        public ContextAwareViewEngine()
        {
            AreaViewLocationFormats = new[]
            {
            "~/Areas/{2}/Views/{1}/{0}.cshtml",
            "~/Areas/{2}/Views/Shared/{0}.cshtml"
            };
            AreaMasterLocationFormats = new[]
             {
            "~/Areas/{2}/Views/{1}/{0}.cshtml",
            "~/Areas/{2}/Views/Shared/{0}.cshtml"
             };
            AreaPartialViewLocationFormats = new[]
             {
            "~/Areas/{2}/Views/{1}/{0}.cshtml",
            "~/Areas/{2}/Views/Shared/{0}.cshtml"
             };
            ViewLocationFormats = new[]
             {
            "~/Views/{1}/{0}.cshtml",
            "~/Views/{0}.cshtml",
            "~/Views/Shared/{0}.cshtml"
             };
            MasterLocationFormats = new[]
             {
            "~/Views/{1}/{0}.cshtml",
            "~/Views/Shared/{0}.cshtml"
             };
            PartialViewLocationFormats = new[]
             {
            "~/Views/{1}/{0}.cshtml",
            "~/Views/{0}.cshtml",
            "~/Views/Shared/{0}.cshtml"
             };
        }
        public override ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            return base.FindPartialView(controllerContext, partialViewName, useCache);
        }

        public override ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            return base.FindView(controllerContext, viewName, masterName, useCache);
        }
    }
}
