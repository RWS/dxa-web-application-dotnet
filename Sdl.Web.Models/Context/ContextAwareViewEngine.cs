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
        public ContextAwareViewEngine()
        {
            var version = Configuration.CurrentVersion;
            AreaViewLocationFormats = new[]
            {
            "~/system/"+version+"/Areas/{2}/Views/{1}/{0}.cshtml",
            "~/system/"+version+"/Areas/{2}/Views/Shared/{0}.cshtml",
            "~/system/Areas/{2}/Views/{1}/{0}.cshtml",
            "~/system/Areas/{2}/Views/Shared/{0}.cshtml"
            };
            AreaMasterLocationFormats = new[]
             {
            "~/system/"+version+"/Areas/{2}/Views/{1}/{0}.cshtml",
            "~/system/"+version+"/Areas/{2}/Views/Shared/{0}.cshtml",
            "~/system/Areas/{2}/Views/{1}/{0}.cshtml",
            "~/system/Areas/{2}/Views/Shared/{0}.cshtml"
             };
            AreaPartialViewLocationFormats = new[]
             {
            "~/system/"+version+"/Areas/{2}/Views/{1}/{0}.cshtml",
            "~/system/"+version+"/Areas/{2}/Views/Shared/{0}.cshtml",
            "~/system/Areas/{2}/Views/{1}/{0}.cshtml",
            "~/system/Areas/{2}/Views/Shared/{0}.cshtml"
             };
            ViewLocationFormats = new[]
             {
            "~/system/"+version+"/Views/{1}/{0}.cshtml",
            "~/system/"+version+"/Views/{0}.cshtml",
            "~/system/"+version+"/Views/Shared/{0}.cshtml",
            "~/system/Views/{1}/{0}.cshtml",
            "~/system/Views/{0}.cshtml",
            "~/system/Views/Shared/{0}.cshtml"
             };
            MasterLocationFormats = new[]
             {
            "~/system/"+version+"/Views/{1}/{0}.cshtml",
            "~/system/"+version+"/Views/Shared/{0}.cshtml",
            "~/system/Views/{1}/{0}.cshtml",
            "~/system/Views/Shared/{0}.cshtml"
             };
            PartialViewLocationFormats = new[]
             {
            "~/system/"+version+"/Views/{1}/{0}.cshtml",
            "~/system/"+version+"/Views/{0}.cshtml",
            "~/system/"+version+"/Views/Shared/{0}.cshtml",
            "~/system/Views/{1}/{0}.cshtml",
            "~/system/Views/{0}.cshtml",
            "~/system/Views/Shared/{0}.cshtml"
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
