using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.Tridion;

namespace Site
{
    public class MvcApplication : HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            RouteTable.Routes.IgnoreRoute("cid/{*pathInfo}");
        }

        protected void Application_Start()
        {
            Configuration.LastApplicationStart = DateTime.Now;
            Configuration.StaticFileManager = new Sdl.Web.DD4T.BinaryFileManager();
            Configuration.SetLocalizations(TridionConfig.PublicationMap);
            Configuration.Load(Server.MapPath("~"));
            
            // load semantic mappings
            SemanticMapping.Load(Server.MapPath("~"));


            RegisterRoutes(RouteTable.Routes);
            AreaRegistration.RegisterAllAreas();
            RegisterGlobalFilters(GlobalFilters.Filters);
            Bootstrapper.Initialise();
            ViewEngines.Engines.Clear();
            //Register Custom Razor View Engine
            ViewEngines.Engines.Add(new ContextAwareViewEngine());
        }
    }
}