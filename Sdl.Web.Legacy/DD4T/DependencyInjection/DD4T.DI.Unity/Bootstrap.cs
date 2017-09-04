using DD4T.ContentModel.Contracts.Caching;
using DD4T.ContentModel.Contracts.Configuration;
using DD4T.Core.Contracts.DependencyInjection;
using DD4T.DI.Unity.Exceptions;
using DD4T.Utils;
using DD4T.Utils.Caching;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DD4T.DI.Unity
{
    public static class Bootstrap
    {
        public static void UseDD4T(this IUnityContainer container)
        {
            //not all dll's are loaded in the app domain. we will load the assembly in the appdomain to be able map the mapping
            var binDirectory = string.Format(@"{0}\bin\", AppDomain.CurrentDomain.BaseDirectory);

            //allowing to register types from any other DD4T.* package into the container:
            //functionality introduced to allow a more plugabble architecture into the framework.            
            var loadedAssemblies = Directory.GetFiles(binDirectory, "DD4T.*.dll").Select(s => Assembly.LoadFile(s));

            var mappers = AppDomain.CurrentDomain.GetAssemblies()
                                   .Where(ass => ass.FullName.StartsWith("DD4T.")||ass.FullName.StartsWith("Sdl.Web.Legacy"))
                                   .SelectMany(s => s.GetTypes())
                                   .Where(p => typeof(IDependencyMapper).IsAssignableFrom(p) && !p.IsInterface)
                                   .Select(o => Activator.CreateInstance(o) as IDependencyMapper).Distinct();

            RegisterMappers(container, mappers);

            if (!Directory.Exists(binDirectory))
                return;

            var file = Directory.GetFiles(binDirectory, "DD4T.Providers.*.dll").FirstOrDefault();
            if (file == null)
                throw new ProviderNotFoundException();

            var load = Assembly.LoadFile(file);

            container.RegisterProviders();
            container.RegisterFactories();
            container.RegisterRestProvider();
            container.RegisterResolvers();
            container.RegisterViewModels();

            if (!container.IsRegistered<IDD4TConfiguration>())
                container.RegisterType<IDD4TConfiguration, DD4TConfiguration>(new ContainerControlledLifetimeManager());

            if (!container.IsRegistered<ICacheAgent>())
                container.RegisterType<ICacheAgent, DefaultCacheAgent>();
        }

        private static void RegisterMappers(IUnityContainer container, IEnumerable<IDependencyMapper> mappers)
        {
            foreach (var mapper in mappers)
            {
                if (mapper.SingleInstanceMappings != null)
                {
                    foreach (var mapping in mapper.SingleInstanceMappings)
                    {
                        if (!container.IsRegistered(mapping.Key))
                            container.RegisterType(mapping.Key, mapping.Value, new ContainerControlledLifetimeManager());
                    }
                }
                if (mapper.PerDependencyMappings != null)
                {
                    foreach (var mapping in mapper.PerDependencyMappings)
                    {
                        if (!container.IsRegistered(mapping.Key))
                            container.RegisterType(mapping.Key, mapping.Value);
                    }
                }
                if (mapper.PerHttpRequestMappings != null)
                {
                    foreach (var mapping in mapper.PerHttpRequestMappings)
                    {
                        if (!container.IsRegistered(mapping.Key))
                            container.RegisterType(mapping.Key, mapping.Value);
                    }
                }
                if (mapper.PerLifeTimeMappings != null)
                {
                    foreach (var mapping in mapper.PerLifeTimeMappings)
                    {
                        if (!container.IsRegistered(mapping.Key))
                            container.RegisterType(mapping.Key, mapping.Value);
                    }
                }
            }
        }
    }
}