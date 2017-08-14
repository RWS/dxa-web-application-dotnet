using Autofac;
using DD4T.ContentModel.Contracts.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.DI.Autofac
{
    public static class Providers
    {
        public static void RegisterProviders(this ContainerBuilder builder)
        {
            var provider = AppDomain.CurrentDomain.GetAssemblies().Where(ass => ass.FullName.StartsWith("DD4T.Providers")).FirstOrDefault();
            var providerTypes = provider.GetTypes();
            var pageprovider = providerTypes.Where(a => typeof(IPageProvider).IsAssignableFrom(a)).FirstOrDefault();
            var cpProvider = providerTypes.Where(a => typeof(IComponentPresentationProvider).IsAssignableFrom(a)).FirstOrDefault();
            var linkProvider = providerTypes.Where(a => typeof(ILinkProvider).IsAssignableFrom(a)).FirstOrDefault();
            var binaryProvider = providerTypes.Where(a => typeof(IBinaryProvider).IsAssignableFrom(a)).FirstOrDefault();
            var componentProvider = providerTypes.Where(a => typeof(IComponentProvider).IsAssignableFrom(a)).FirstOrDefault();
            var commonServices = providerTypes.Where(a => typeof(IProvidersCommonServices).IsAssignableFrom(a)).FirstOrDefault();


            if (commonServices != null)
                builder.RegisterType(commonServices).As<IProvidersCommonServices>().PreserveExistingDefaults();
            if (pageprovider != null)
                builder.RegisterType(pageprovider).As<IPageProvider>().PreserveExistingDefaults();
            if (cpProvider != null)
                builder.RegisterType(cpProvider).As<IComponentPresentationProvider>().PreserveExistingDefaults();
            if (binaryProvider != null)
                builder.RegisterType(binaryProvider).As<IBinaryProvider>().PreserveExistingDefaults();
            if (linkProvider != null)
                builder.RegisterType(linkProvider).As<ILinkProvider>().PreserveExistingDefaults();
            if (componentProvider != null)
                builder.RegisterType(componentProvider).As<IComponentProvider>().PreserveExistingDefaults();
        }
    }
}
