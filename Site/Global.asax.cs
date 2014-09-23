using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Tridion.Config;
using Unity.Mvc5;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Site.Areas.Core.Controllers;

namespace Sdl.Web.Site
{
    public class MvcApplication : HttpApplication
    {
        private static bool initialized = false;
        public static void RegisterRoutes(RouteCollection routes)
        {
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            RouteTable.Routes.IgnoreRoute("cid/{*pathInfo}");
            RouteTable.Routes.MapMvcAttributeRoutes();
            //Navigation JSON
            routes.MapRoute(
                "Core_Blank",
                "se_blank.html",
                new { controller = "Page", action = "Blank" }
            ).DataTokens.Add("area", "Core");

            //Navigation JSON
            routes.MapRoute(
                "Core_Navigation",
                "navigation.json",
                new { controller = "Page", action = "PageRaw" }
            ).DataTokens.Add("area", "Core");

            //Navigation JSON
            routes.MapRoute(
                "Core_Navigation_loc",
                "{localization}/navigation.json",
                new { controller = "Page", action = "PageRaw" }
            ).DataTokens.Add("area", "Core");

            //Google Site Map
            routes.MapRoute(
                "Core_Sitemap",
                "sitemap.xml",
                new { controller = "Navigation", action = "SiteMap" }
            ).DataTokens.Add("area", "Core");

            //Google Site Map
            routes.MapRoute(
                "Core_Sitemap_Loc",
                "{localization}/sitemap.xml",
                new { controller = "Navigation", action = "SiteMap" }
            ).DataTokens.Add("area", "Core");
        
            //For resolving ids to urls
            routes.MapRoute(
               "Core_Resolve",
               "resolve/{*itemId}",
               new { controller = "Page", action = "Resolve" },
               new { itemId = @"^(.*)?$" }
            ).DataTokens.Add("area", "Core");
            
            //Tridion Page Route
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
            SiteConfiguration.StaticFileManager = (IStaticFileManager)DependencyResolver.Current.GetService(typeof(IStaticFileManager));
            SiteConfiguration.MediaHelper = (IMediaHelper)DependencyResolver.Current.GetService(typeof(IMediaHelper));
            SiteConfiguration.Initialize(TridionConfig.PublicationMap);
            RegisterRoutes(RouteTable.Routes);
            AreaRegistration.RegisterAllAreas();
            initialized = true;
        }

        protected IUnityContainer InitializeDependencyInjection()
        {
            var container = BuildUnityContainer();
            DependencyResolver.SetResolver(new UnityDependencyResolver(container));
            return container;
        }

        protected IUnityContainer BuildUnityContainer()
        {
            var section = (UnityConfigurationSection)System.Configuration.ConfigurationManager.GetSection("unity");
            var container = section.Configure(new UnityContainer(), "main");
            ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(container));
            return container;
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            if (Context.IsCustomErrorEnabled && initialized)
                ShowCustomErrorPage(Server.GetLastError());
        }

        private void ShowCustomErrorPage(Exception exception)
        {
            HttpException httpException = exception as HttpException;
            if (httpException == null)
                httpException = new HttpException(500, "Internal Server Error", exception);

            RouteData routeData = new RouteData();
            Log.Error(httpException);
            routeData.Values.Add("controller", "Page");
            routeData.Values.Add("area", "Core");
            routeData.Values.Add("action", "ServerError");
            Server.ClearError();
            IController controller = new PageController(null,null);
            controller.Execute(new RequestContext(new HttpContextWrapper(Context), routeData));
        }
    }
}