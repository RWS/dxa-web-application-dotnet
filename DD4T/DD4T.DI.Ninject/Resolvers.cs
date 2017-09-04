using DD4T.ContentModel.Contracts.Resolvers;
using DD4T.Core.Contracts.Resolvers;
using DD4T.Core.Contracts.ViewModels;
using DD4T.Utils.Resolver;
using DD4T.ViewModels;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.DI.Ninject
{
    public static class Resolvers
    {
        public static void BindResolvers(this IKernel kernel)
        {

            if (kernel.TryGet<IPublicationResolver>() == null)
                kernel.Bind<IPublicationResolver>().To<DefaultPublicationResolver>().InSingletonScope();

            if (kernel.TryGet<IRichTextResolver>() == null)
                kernel.Bind<IRichTextResolver>().To<DefaultRichTextResolver>().InSingletonScope();

            if (kernel.TryGet<IContextResolver>() == null)
                kernel.Bind<IContextResolver>().To<DefaultContextResolver>().InSingletonScope();

            if (kernel.TryGet<ILinkResolver>() == null)
                kernel.Bind<ILinkResolver>().To<DefaultLinkResolver>().InSingletonScope();
            
        
        }
    }
}
