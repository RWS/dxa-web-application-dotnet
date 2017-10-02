using Autofac;
using Autofac.Integration.WebApi;
using DD4T.ContentModel.Contracts.Configuration;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Contracts.Resolvers;
using DD4T.DI.Autofac;
using DD4T.Utils;
using DD4T.Utils.Logging;
using DD4T.Utils.Resolver;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace DD4T.RestService.WebApi
{
    public static class AppBuilderExtensions
    {

        public static void UseDD4TWebApi(this IAppBuilder appBuilder)
        {
            var builder = new ContainerBuilder();
            appBuilder.UseDD4TWebApi(builder);
        }

        public static void UseDD4TWebApi(this IAppBuilder appBuilder, HttpConfiguration config)
        {
            var builder = new ContainerBuilder();
            appBuilder.UseDD4TWebApi(builder, config);
        }

        public static void UseDD4TWebApi(this IAppBuilder appBuilder, ContainerBuilder builder)
        {
            var config = new HttpConfiguration();
            appBuilder.UseDD4TWebApi(builder, config);
        }

        public static void UseDD4TWebApi(this IAppBuilder appBuilder, ContainerBuilder builder, HttpConfiguration config)
        {
            builder.RegisterApiControllers(typeof(AppBuilderExtensions).Assembly);
            builder.UseDD4T();

            var container = builder.Build();

            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            config.MapHttpAttributeRoutes();

            appBuilder.UseAutofacMiddleware(container);
            appBuilder.UseAutofacWebApi(config);
            appBuilder.UseWebApi(config);
        }
    }
}
