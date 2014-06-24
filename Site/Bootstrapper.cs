using System.Web.Mvc;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using Unity.Mvc5;

namespace Site
{
  public static class Bootstrapper
  {
    public static IUnityContainer Initialise()
    {
      var container = BuildUnityContainer();

      DependencyResolver.SetResolver(new UnityDependencyResolver(container));

      return container;
    }

    private static IUnityContainer BuildUnityContainer()
    {
      var section = (UnityConfigurationSection)System.Configuration.ConfigurationManager.GetSection("unity");
      var container = section.Configure(new UnityContainer(), "main");
      ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(container));
      RegisterTypes(container);
      return container;
    }

    public static void RegisterTypes(IUnityContainer container)
    {
    
    }
  }
}