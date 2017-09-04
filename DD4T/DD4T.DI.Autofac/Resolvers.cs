using Autofac;
using DD4T.ContentModel.Contracts.Resolvers;
using DD4T.Core.Contracts.Resolvers;
using DD4T.Core.Contracts.ViewModels;
using DD4T.Utils.Resolver;
using DD4T.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.DI.Autofac
{
    public static class Resolvers
    {
        public static void RegisterResolvers(this ContainerBuilder builder)
        {
            builder.RegisterType<DefaultLinkResolver>()
                .As<ILinkResolver>()
                .SingleInstance()
                .PreserveExistingDefaults(); 

            builder.RegisterType<DefaultRichTextResolver>()
                .As<IRichTextResolver>()
                .SingleInstance()
                .PreserveExistingDefaults();

            builder.RegisterType<DefaultContextResolver>()
                .As<IContextResolver>()
                .SingleInstance()
                .PreserveExistingDefaults();

            builder.RegisterType<DefaultPublicationResolver>()
                .As<IPublicationResolver>()
                .SingleInstance()
                .PreserveExistingDefaults();

        }
    }
}
