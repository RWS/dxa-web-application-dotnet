using DD4T.ContentModel.Factories;
using DD4T.Factories;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.DI.Unity
{
    public static class Factories
    {
        public static void RegisterFactories(this IUnityContainer container)
        {
            //factories
            if (!container.IsRegistered<IPageFactory>())
                container.RegisterType<IPageFactory, PageFactory>();

            if (!container.IsRegistered<IComponentPresentationFactory>())
                container.RegisterType<IComponentPresentationFactory, ComponentPresentationFactory>();

            if (!container.IsRegistered<ILinkFactory>())
                container.RegisterType<ILinkFactory, LinkFactory>();

            if (!container.IsRegistered<IBinaryFactory>())
                container.RegisterType<IBinaryFactory, BinaryFactory>();

            if (!container.IsRegistered<IComponentFactory>())
                container.RegisterType<IComponentFactory, ComponentFactory>();

            if (!container.IsRegistered<IFactoryCommonServices>())
                container.RegisterType<IFactoryCommonServices, FactoryCommonServices>();

        }
    }
}
