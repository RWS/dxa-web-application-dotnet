using DD4T.ContentModel.Contracts.Caching;
using DD4T.ContentModel.Contracts.Configuration;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.ContentModel.Factories;
using Sdl.Web.Common.Configuration;
using System.Web.Mvc;

namespace Sdl.Web.Tridion.Mapping
{
    /// <summary>
    /// Cache for DD4T Factories; one for each Localization.
    /// </summary>
    internal static class DD4TFactory
    {
        internal static IDD4TConfiguration Configuration()
        {
            return (IDD4TConfiguration)DependencyResolver.Current.GetService(typeof(IDD4TConfiguration));
        }

        internal static ICacheAgent CacheAgent()
        {
            return (ICacheAgent)DependencyResolver.Current.GetService(typeof(ICacheAgent));
        }

        internal static ILogger Logger()
        {
            return (ILogger)DependencyResolver.Current.GetService(typeof(ILogger));
        }    

        internal static IPageFactory GetPageFactory(Localization localization)
        {
            return (IPageFactory)DependencyResolver.Current.GetService(typeof(IPageFactory));
        }

        internal static IComponentPresentationFactory GetComponentPresentationFactory(Localization localization)
        {
            return (IComponentPresentationFactory)DependencyResolver.Current.GetService(typeof(IComponentPresentationFactory));
        }

        internal static IComponentFactory GetComponentFactory(Localization localization)
        {
            return (IComponentFactory)DependencyResolver.Current.GetService(typeof(IComponentFactory));
        }

        internal static IBinaryFactory GetBinaryFactory(Localization localization)
        {
            return (IBinaryFactory)DependencyResolver.Current.GetService(typeof(IBinaryFactory));
        }
    }
}
