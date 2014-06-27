using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.Tridion;
using Microsoft.Practices.Unity;
using Unity.Mvc5;
using Microsoft.Practices.Unity.Configuration;
using Microsoft.Practices.ServiceLocation;
using Sdl.Web.Mvc.Html;

namespace Sdl.Web.Site
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
            InitializeDI();
            //TODO -can this be handled by DI?
            Configuration.StaticFileManager = new Sdl.Web.DD4T.BinaryFileManager();
            //Configuration.MediaHelper = new Sdl.Web.DD4T.Html.DD4TMediaHelper();
            Configuration.MediaHelper = new Sdl.Web.Mvc.Html.ContextualMediaHelper();
            Configuration.Initialize(Server.MapPath("~"), TridionConfig.PublicationMap);
            RegisterRoutes(RouteTable.Routes);
            AreaRegistration.RegisterAllAreas();
            RegisterGlobalFilters(GlobalFilters.Filters);
        }

        protected IUnityContainer InitializeDI()
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
    }
}