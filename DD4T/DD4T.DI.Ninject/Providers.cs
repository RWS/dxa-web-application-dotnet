using DD4T.ContentModel.Contracts.Providers;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.DI.Ninject
{
    public static class Providers
    {
        public static void BindProviders(this IKernel kernel)
        {
            var provider = AppDomain.CurrentDomain.GetAssemblies().Where(ass => ass.FullName.StartsWith("DD4T.Providers")).FirstOrDefault();
            var providerTypes = provider.GetTypes();
            var pageprovider = providerTypes.Where(a => typeof(IPageProvider).IsAssignableFrom(a)).FirstOrDefault();
            var cpProvider = providerTypes.Where(a => typeof(IComponentPresentationProvider).IsAssignableFrom(a)).FirstOrDefault();
            var linkProvider = providerTypes.Where(a => typeof(ILinkProvider).IsAssignableFrom(a)).FirstOrDefault();
            var commonServices = providerTypes.Where(a => typeof(IProvidersCommonServices).IsAssignableFrom(a)).FirstOrDefault();
            var binaryProvider = providerTypes.Where(a => typeof(IBinaryProvider).IsAssignableFrom(a)).FirstOrDefault();
            var componentProvider = providerTypes.Where(a => typeof(IComponentProvider).IsAssignableFrom(a)).FirstOrDefault();

            //providers
            if (binaryProvider != null && kernel.TryGet<IBinaryProvider>() == null)
                kernel.Bind<IBinaryProvider>().To(binaryProvider);

            if (componentProvider != null && kernel.TryGet<IComponentProvider>() == null)
                kernel.Bind<IComponentProvider>().To(componentProvider);

            if (pageprovider != null && kernel.TryGet<IPageProvider>() == null)
                kernel.Bind<IPageProvider>().To(pageprovider);

            if (cpProvider != null && kernel.TryGet<IComponentPresentationProvider>() == null)
                kernel.Bind<IComponentPresentationProvider>().To(cpProvider);

            if (linkProvider != null && kernel.TryGet<ILinkProvider>() == null)
                kernel.Bind<ILinkProvider>().To(linkProvider);

            if (commonServices != null && kernel.TryGet<IProvidersCommonServices>() == null)
                kernel.Bind<IProvidersCommonServices>().To(commonServices);
        }
    }
}
