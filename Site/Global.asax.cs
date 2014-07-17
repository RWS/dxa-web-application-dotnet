using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Sdl.Web.Mvc;
using Sdl.Web.Tridion;
using Microsoft.Practices.Unity;
using Unity.Mvc5;
using Microsoft.Practices.Unity.Configuration;
using Microsoft.Practices.ServiceLocation;
using Sdl.Web.Mvc.Html;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common;

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
            RouteTable.Routes.MapMvcAttributeRoutes();
        }

        protected void Application_Start()
        {
            InitializeDI();
            Configuration.StaticFileManager = (IStaticFileManager)DependencyResolver.Current.GetService(typeof(IStaticFileManager));
            Configuration.MediaHelper = (IMediaHelper)DependencyResolver.Current.GetService(typeof(IMediaHelper));
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