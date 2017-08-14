using DD4T.ContentModel.Contracts.Configuration;
using DD4T.ContentModel.Contracts.Resolvers;
using DD4T.ContentModel.Factories;
using DD4T.ContentModel.Contracts.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DD4T.ContentModel.Contracts.Caching;
using DD4T.ContentModel.Contracts.Serializing;

namespace DD4T.Factories
{
    public class FactoryCommonServices : IFactoryCommonServices
    {
        public IPublicationResolver PublicationResolver { get; private set; }
        public ILogger Logger { get; private set; }
        public IDD4TConfiguration Configuration { get; private set; }
        public ICacheAgent CacheAgent { get; private set; }
        public ISerializerService SerializerService { get; private set; }

        public FactoryCommonServices(IPublicationResolver resolver, ILogger logger,
                        IDD4TConfiguration configuration, ICacheAgent cacheAgent)
        {
            if (resolver == null)
                throw new ArgumentNullException("resolver");

            if (logger == null)
                throw new ArgumentNullException("logger");

            if (configuration == null)
                throw new ArgumentNullException("configuration");

            if (cacheAgent == null)
                throw new ArgumentNullException("cacheAgent");

            Logger = logger;
            PublicationResolver = resolver;
            Configuration = configuration;
            CacheAgent = cacheAgent;
        }
       
    }
}
