using DD4T.ContentModel.Contracts.Providers;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.DI.Unity
{
    public static class Providers
    {
        public static void RegisterProviders(this IUnityContainer container)
        {
            var provider = AppDomain.CurrentDomain.GetAssemblies().Where(ass => ass.FullName.StartsWith("DD4T.Providers")).FirstOrDefault();
            var providerTypes = provider.GetTypes();
            var pageProvider = providerTypes.Where(a => typeof(IPageProvider).IsAssignableFrom(a)).FirstOrDefault();
            var cpProvider = providerTypes.Where(a => typeof(IComponentPresentationProvider).IsAssignableFrom(a)).FirstOrDefault();
            var linkProvider = providerTypes.Where(a => typeof(ILinkProvider).IsAssignableFrom(a)).FirstOrDefault();
            var binaryProvider = providerTypes.Where(a => typeof(IBinaryProvider).IsAssignableFrom(a)).FirstOrDefault();
            var componentProvider = providerTypes.Where(a => typeof(IComponentProvider).IsAssignableFrom(a)).FirstOrDefault();
            var commonServices = providerTypes.Where(a => typeof(IProvidersCommonServices).IsAssignableFrom(a)).FirstOrDefault();

            //providers
            if (binaryProvider != null && !container.IsRegistered<IBinaryProvider>())
                container.RegisterType(typeof(IBinaryProvider), binaryProvider);

            if (componentProvider != null && !container.IsRegistered<IComponentProvider>())
                container.RegisterType(typeof(IComponentProvider), componentProvider);

            if (pageProvider != null && !container.IsRegistered<IPageProvider>())
                container.RegisterType(typeof(IPageProvider), pageProvider);

            if (cpProvider != null && !container.IsRegistered<IComponentPresentationProvider>())
                container.RegisterType(typeof(IComponentPresentationProvider), cpProvider);

            if (linkProvider != null && !container.IsRegistered<ILinkProvider>())
                container.RegisterType(typeof(ILinkProvider), linkProvider);

            if (commonServices != null && !container.IsRegistered<IProvidersCommonServices>())
                container.RegisterType(typeof(IProvidersCommonServices), commonServices);
        }
    }
}
