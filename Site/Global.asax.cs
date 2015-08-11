using System;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Mvc.Formats;
using Sdl.Web.Site.Areas.Core.Controllers;
using Unity.Mvc5;

namespace Sdl.Web.Site
{
    public class MvcApplication : HttpApplication
    {
        private static bool _initialized;

        public static void RegisterRoutes(RouteCollection routes)
        {
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            RouteTable.Routes.IgnoreRoute("cid/{*pathInfo}");
            RouteTable.Routes.MapMvcAttributeRoutes();
            
            // XPM blank page
            routes.MapRoute(
                "Core_Blank",
                "se_blank.html",
                new { controller = "Page", action = "Blank" }
            ).DataTokens.Add("area","Core");

            // Navigation JSON
            routes.MapRoute(
                "Core_Navigation",
                "navigation.json",
                new { controller = "Navigation", action = "SiteMapJson" }
            ).DataTokens.Add("area", "Core");
            routes.MapRoute(
                "Core_Navigation_loc",
                "{localization}/navigation.json",
                new { controller = "Navigation", action = "SiteMapJson" }
            ).DataTokens.Add("area", "Core");

            // Google Site Map
            routes.MapRoute(
                "Core_Sitemap",
                "sitemap.xml",
                new { controller = "Navigation", action = "SiteMapXml" }
            ).DataTokens.Add("area", "Core");
            routes.MapRoute(
                "Core_Sitemap_Loc",
                "{localization}/sitemap.xml",
                new { controller = "Navigation", action = "SiteMapXml" }
            ).DataTokens.Add("area", "Core");

            // For resolving ids to urls
            routes.MapRoute(
               "Core_Resolve",
               "resolve/{*itemId}",
               new { controller = "Page", action = "Resolve" },
               new { itemId = @"^(.*)?$" }
            ).DataTokens.Add("area", "Core");
            routes.MapRoute(
               "Core_Resolve_Loc",
               "{localization}/resolve/{*itemId}",
               new { controller = "Page", action = "Resolve" },
               new { itemId = @"^(.*)?$" }
            ).DataTokens.Add("area", "Core");

            // Admin actions
            string enable = WebConfigurationManager.AppSettings["admin.refresh.enabled"];
            if (!String.IsNullOrEmpty(enable) && enable.Equals("true", StringComparison.InvariantCultureIgnoreCase))
            {
                routes.MapRoute(
                   "Core_Admin",
                   "admin/{action}",
                   new { controller = "Admin", action = "Refresh" }
                ).DataTokens.Add("area", "Core");
                routes.MapRoute(
                   "Core_Admin_Loc",
                   "{localization}/admin/{action}",
                   new { controller = "Admin", action = "Refresh" }
                ).DataTokens.Add("area", "Core");                
            }

            // Tridion Page Route
            routes.MapRoute(
               "Core_Page",
               "{*pageUrl}",
               new { controller = "Page", action = "Page" },
               new { pageId = @"^(.*)?$" }
            ).DataTokens.Add("area", "Core");
        }

        protected void Application_Start()
        {
            InitializeDependencyInjection();
            SiteConfiguration.InitializeProviders(DependencyResolver.Current);

            DataFormatters.Formatters.Add("json", new JsonFormatter());
            DataFormatters.Formatters.Add("rss", new RssFormatter());
            DataFormatters.Formatters.Add("atom", new AtomFormatter());
            
            RegisterRoutes(RouteTable.Routes);
            AreaRegistration.RegisterAllAreas();
            _initialized = true;
        }

        protected IUnityContainer InitializeDependencyInjection()
        {
            IUnityContainer container = BuildUnityContainer();
            DependencyResolver.SetResolver(new UnityDependencyResolver(container));
            return container;
        }

        protected IUnityContainer BuildUnityContainer()
        {
            UnityConfigurationSection section = (UnityConfigurationSection)System.Configuration.ConfigurationManager.GetSection("unity");
            IUnityContainer container = section.Configure(new UnityContainer(), "main");
            ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(container));
            return container;
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            if (Context.IsCustomErrorEnabled && _initialized)
            {
                ShowCustomErrorPage(Server.GetLastError());
            }
        }

        private void ShowCustomErrorPage(Exception exception)
        {
            HttpException httpException = exception as HttpException;
            if (httpException == null)
            {
                httpException = new HttpException(500, "Internal Server Error", exception);
            }

            RouteData routeData = new RouteData();
            Log.Error(httpException);
            routeData.Values.Add("controller", "Page");
            routeData.Values.Add("area", "Core");
            routeData.Values.Add("action", "ServerError");
            Server.ClearError();
            IController controller = new PageController();
            controller.Execute(new RequestContext(new HttpContextWrapper(Context), routeData));
        }
    }
}
