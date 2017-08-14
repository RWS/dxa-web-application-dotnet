using DD4T.ContentModel.Contracts.Caching;
using DD4T.ContentModel.Contracts.Configuration;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Contracts.Resolvers;
using DD4T.ContentModel.Factories;
using DD4T.Core.Contracts.DependencyInjection;
using DD4T.Core.Contracts.ViewModels;
using DD4T.DI.Ninject.Exceptions;
using DD4T.Factories;
using DD4T.Utils;
using DD4T.Utils.Caching;
using DD4T.Utils.Logging;
using DD4T.Utils.Resolver;
using DD4T.ViewModels;
using DD4T.ViewModels.Reflection;
using Ninject;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.DI.Ninject
{
    public static class Bootstrap
    {
        public static void UseDD4T(this IKernel kernel)
        {
            var binDirectory = string.Format(@"{0}\bin\", AppDomain.CurrentDomain.BaseDirectory);
            //allowing to register types from any other DD4T.* package into the container:
            //functionality introduced to allow a more plugabble architecture into the framework.
            var loadedAssemblies = Directory.GetFiles(binDirectory, "DD4T.*").Select(s => Assembly.LoadFile(s));

            var mappers = AppDomain.CurrentDomain.GetAssemblies()
                                   .Where(ass => ass.FullName.StartsWith("DD4T."))
                                   .SelectMany(s => s.GetTypes())
                                   .Where(p => typeof(IDependencyMapper).IsAssignableFrom(p) && !p.IsInterface)
                                   .Select(o => Activator.CreateInstance(o) as IDependencyMapper).Distinct();

            foreach (var mapper in mappers)
            {
                if (mapper.SingleInstanceMappings != null)
                {
                    foreach (var mapping in mapper.SingleInstanceMappings)
                    {
                        if (kernel.TryGet(mapping.Key) == null)
                            kernel.Bind(mapping.Key).To(mapping.Value).InSingletonScope();
                    }
                }
                if (mapper.PerDependencyMappings != null)
                {
                    foreach (var mapping in mapper.PerDependencyMappings)
                    {
                        if (kernel.TryGet(mapping.Key) == null)
                            kernel.Bind(mapping.Key).To(mapping.Value);
                    }
                }
                if (mapper.PerHttpRequestMappings != null)
                {
                    foreach (var mapping in mapper.PerHttpRequestMappings)
                    {
                        if (kernel.TryGet(mapping.Key) == null)
                            kernel.Bind(mapping.Key).To(mapping.Value).InThreadScope();
                    }
                }
                if (mapper.PerLifeTimeMappings != null)
                {
                    foreach (var mapping in mapper.PerLifeTimeMappings)
                    {
                        if (kernel.TryGet(mapping.Key) == null)
                            kernel.Bind(mapping.Key).To(mapping.Value).InTransientScope();
                    }
                }
            }

            //not all dll's are loaded in the app domain. we will load the assembly in the appdomain to be able map the mapping
            if (!Directory.Exists(binDirectory))
                return;

            var file = Directory.GetFiles(binDirectory, "DD4T.Providers.*").FirstOrDefault();
            if (file == null)
                throw new ProviderNotFoundException();

            var load = Assembly.LoadFile(file);

            kernel.BindProviders();
            kernel.BindFactories();
            kernel.BindRestProvider();
            kernel.BindResolvers();
            kernel.BindViewModels();

            if (kernel.TryGet<IDD4TConfiguration>() == null)
                kernel.Bind<IDD4TConfiguration>().To<DD4TConfiguration>().InSingletonScope();

            if (kernel.TryGet<ICacheAgent>() == null)
                kernel.Bind<ICacheAgent>().To<DefaultCacheAgent>();
        }
    }
}