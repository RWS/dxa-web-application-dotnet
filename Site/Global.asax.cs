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
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            
            routes.IgnoreRoute("cid/{*pathInfo}");

            //Google Site Map
            routes.MapRoute(
                "sitemap",
                "sitemap",
                new { controller = "Navigation", action = "GoogleSitemap" }
            );

            //Tridion Page Route
            routes.MapRoute(
               "TridionPage",
               "{*pageUrl}",
               new { controller = "Page", action = "Page" },
               new { pageId = @"^(.*)?$" }
            );

            //Default Route - required for sub actions (region/entity/navigation etc.)
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }

        protected void Application_Start()
        {
            Configuration.StaticFileManager = new Sdl.Web.DD4T.BinaryFileManager();
            Configuration.SetLocalizations(TridionConfig.PublicationMap);
            var currentVersion = Configuration.CurrentVersion;
            Configuration.Load(Server.MapPath("~"));
            
            // load semantic mappings
            SemanticMapping.Load(Server.MapPath("~"));

            AreaRegistration.RegisterAllAreas();
            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
            Bootstrapper.Initialise();
            ViewEngines.Engines.Clear();
            //Register Custom Razor View Engine
            ViewEngines.Engines.Add(new ContextAwareViewEngine());
        }
    }
}