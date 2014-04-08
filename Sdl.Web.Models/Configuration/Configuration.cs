using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc
{
    public static class Configuration
    {
        public static string GetDefaultPageName()
        {
            return ConfigurationManager.AppSettings["Sdl.Web.DefaultPage"] ?? "index.html";
        }
        public static string GetDefaultExtension()
        {
            return ConfigurationManager.AppSettings["Sdl.Web.DefaultPageExtension"] ?? "";
        }
        public static string GetRegionController()
        {
            return ConfigurationManager.AppSettings["Sdl.Web.RegionController"] ?? "Region";
        }
        public static string GetRegionAction()
        {
            return ConfigurationManager.AppSettings["Sdl.Web.RegionAction"] ?? "Region";
        }
        public static string GetCmsUrl()
        {
            return ConfigurationManager.AppSettings["Sdl.Web.CmsUrl"];
        }
    }
}
