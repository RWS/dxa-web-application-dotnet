using DD4T.ContentModel.Factories;
using DD4T.Factories;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.DI.Ninject
{
    public static class Factories
    {
        public static void BindFactories(this IKernel kernel)
        {
            //factories
            if (kernel.TryGet<IPageFactory>() == null)
                kernel.Bind<IPageFactory>().To<PageFactory>();

            if (kernel.TryGet<IComponentPresentationFactory>() == null)
                kernel.Bind<IComponentPresentationFactory>().To<ComponentPresentationFactory>();

            if (kernel.TryGet<ILinkFactory>() == null)
                kernel.Bind<ILinkFactory>().To<LinkFactory>();

            if (kernel.TryGet<IBinaryFactory>() == null)
                kernel.Bind<IBinaryFactory>().To<BinaryFactory>();

            if (kernel.TryGet<IComponentFactory>() == null)
                kernel.Bind<IComponentFactory>().To<ComponentFactory>();

            if (kernel.TryGet<IFactoryCommonServices>() == null)
                kernel.Bind<IFactoryCommonServices>().To<FactoryCommonServices>();
        }
    }
}
