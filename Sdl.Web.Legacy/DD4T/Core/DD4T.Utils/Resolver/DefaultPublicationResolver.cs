using DD4T.ContentModel.Contracts.Configuration;
using DD4T.ContentModel.Contracts.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DD4T.Utils.Resolver
{
    public class DefaultPublicationResolver : IPublicationResolver
    {
        private readonly IDD4TConfiguration Configuration;

        public DefaultPublicationResolver(IDD4TConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentException("configuration");

            Configuration = configuration;
        }

        public int ResolvePublicationId()
        {
            return Configuration.PublicationId;
        }
       
    }
}
