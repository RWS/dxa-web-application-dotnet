using Autofac;
using DD4T.ContentModel.Contracts.Caching;
using DD4T.ContentModel.Contracts.Configuration;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Contracts.Resolvers;
using DD4T.ContentModel.Factories;
using DD4T.Core.Contracts.DependencyInjection;
using DD4T.Core.Contracts.ViewModels;
using DD4T.DI.Autofac.Exceptions;
using DD4T.Factories;
using DD4T.Utils;
using DD4T.Utils.Caching;
using DD4T.Utils.Logging;
using DD4T.Utils.Resolver;
using DD4T.ViewModels;
using DD4T.ViewModels.Reflection;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DD4T.DI.Autofac
{
    public static class Bootstrap
    {
        public static void UseDD4T(this ContainerBuilder builder)
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
                        builder.RegisterType(mapping.Value).As(mapping.Key).SingleInstance().PreserveExistingDefaults();
                    }
                }
                if (mapper.PerDependencyMappings != null)
                {
                    foreach (var mapping in mapper.PerDependencyMappings)
                    {
                        builder.RegisterType(mapping.Value).As(mapping.Key).InstancePerDependency().PreserveExistingDefaults();
                    }
                }
                if (mapper.PerHttpRequestMappings != null)
                {
                    foreach (var mapping in mapper.PerHttpRequestMappings)
                    {
                        builder.RegisterType(mapping.Value).As(mapping.Key).InstancePerRequest().PreserveExistingDefaults();
                    }
                }
                if (mapper.PerLifeTimeMappings != null)
                {
                    foreach (var mapping in mapper.PerLifeTimeMappings)
                    {
                        builder.RegisterType(mapping.Value).As(mapping.Key).InstancePerLifetimeScope().PreserveExistingDefaults();
                    }
                }
            }

            //not all dll's are loaded in the app domain. we will load the assembly in the appdomain to be able map the mapping
            //var binDirectory = string.Format(@"{0}\bin\", AppDomain.CurrentDomain.BaseDirectory);
            if (!Directory.Exists(binDirectory))
                return;

            var file = Directory.GetFiles(binDirectory, "DD4T.Providers.*").FirstOrDefault();
            if (file == null)
                throw new ProviderNotFoundException();

            var load = Assembly.LoadFile(file);

            builder.RegisterProviders();
            builder.RegisterFactories();
            builder.RegisterRestProvider();
            //builder.RegisterMvc();
            builder.RegisterResolvers();
            builder.RegisterViewModels();

            builder.RegisterType<DD4TConfiguration>().As<IDD4TConfiguration>().SingleInstance().PreserveExistingDefaults();

            builder.RegisterType<DefaultCacheAgent>().As<ICacheAgent>().PreserveExistingDefaults();

            //caching JMS
            //builder.RegisterType<JMSMessageProvider>()
            //    .As<IMessageProvider>()
            //    .SingleInstance()
            //    .PreserveExistingDefaults();
        }
    }
}