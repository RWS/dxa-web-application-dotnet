using DD4T.ContentModel.Contracts.Configuration;
using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Contracts.Resolvers;
using DD4T.ContentModel.Factories;
using DD4T.ContentModel.Contracts.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.Provider.SDLTridion2011sp1
{
    public class ProvidersCommonServices : IProvidersCommonServices
    {
        public IPublicationResolver PublicationResolver { get; private set; }
        public ILogger Logger { get; private set; }
        public IDD4TConfiguration Configuration { get; private set; }

        public ProvidersCommonServices(IPublicationResolver resolver, ILogger logger, IDD4TConfiguration configuration)
        {
            if (resolver == null)
                throw new ArgumentNullException("resolver");

            if (logger == null)
                throw new ArgumentNullException("logger");

            if (configuration == null)
                throw new ArgumentNullException("configuration");

            Logger = logger;
            PublicationResolver = resolver;
            Configuration = configuration;
        }
    }
}