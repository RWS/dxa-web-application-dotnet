using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.DI.Autofac
{
    public static class RestProvider
    {
        public static void RegisterRestProvider(this ContainerBuilder builder)
        {
            var provider = AppDomain.CurrentDomain.GetAssemblies().Where(ass => ass.FullName.StartsWith("DD4T.Providers.Rest")).FirstOrDefault();
            if (provider == null)
                return;

            var providerTypes = provider.GetTypes();

            var iHttpMessageHandlerFactory = providerTypes.Where(a => a.FullName.Equals("DD4T.Providers.Rest.IHttpMessageHandlerFactory")).FirstOrDefault();
            var defaultHttpMessageHandlerFactory = providerTypes.Where(a => a.FullName.Equals("DD4T.Providers.Rest.DefaultHttpMessageHandlerFactory")).FirstOrDefault();

            if (iHttpMessageHandlerFactory == null || defaultHttpMessageHandlerFactory == null)
                return;

            builder.RegisterType(defaultHttpMessageHandlerFactory).As(new[] { iHttpMessageHandlerFactory }).PreserveExistingDefaults();
        }
    }
}
