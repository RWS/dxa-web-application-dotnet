using DD4T.ContentModel.Contracts.Resolvers;
using DD4T.Core.Contracts.Resolvers;
using DD4T.Core.Contracts.ViewModels;
using DD4T.Utils.Resolver;
using DD4T.ViewModels;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.DI.Unity
{
    public static class Resolvers
    {
        public static void RegisterResolvers(this IUnityContainer container)
        {

            if (!container.IsRegistered<IPublicationResolver>())
                container.RegisterType<IPublicationResolver, DefaultPublicationResolver>(new ContainerControlledLifetimeManager());

            if (!container.IsRegistered<ILinkResolver>())
                container.RegisterType<ILinkResolver, DefaultLinkResolver>(new ContainerControlledLifetimeManager());

            if (!container.IsRegistered<IRichTextResolver>())
                container.RegisterType<IRichTextResolver, DefaultRichTextResolver>(new ContainerControlledLifetimeManager());

            if (!container.IsRegistered<IContextResolver>())
                container.RegisterType<IContextResolver, DefaultContextResolver>(new ContainerControlledLifetimeManager());

        }
    }
}
