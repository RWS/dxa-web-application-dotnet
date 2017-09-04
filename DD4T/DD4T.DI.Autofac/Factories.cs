using Autofac;
using DD4T.ContentModel.Factories;
using DD4T.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.DI.Autofac
{
    public static class Factories
    {
        public static void RegisterFactories(this ContainerBuilder builder)
        {
            builder.RegisterType<FactoryCommonServices>().As<IFactoryCommonServices>().PreserveExistingDefaults();
            builder.RegisterType<PageFactory>().As<IPageFactory>().PreserveExistingDefaults();
            builder.RegisterType<ComponentPresentationFactory>().As<IComponentPresentationFactory>().PreserveExistingDefaults();
            builder.RegisterType<ComponentFactory>().As<IComponentFactory>().PreserveExistingDefaults();
            builder.RegisterType<BinaryFactory>().As<IBinaryFactory>().PreserveExistingDefaults();
            builder.RegisterType<LinkFactory>().As<ILinkFactory>().PreserveExistingDefaults();
        }
    }
}
