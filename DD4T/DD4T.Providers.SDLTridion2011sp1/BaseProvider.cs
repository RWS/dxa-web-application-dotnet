using DD4T.ContentModel.Contracts.Configuration;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Contracts.Resolvers;
using System;

namespace DD4T.Providers.SDLTridion2011sp1
{
    public class BaseProvider : IProvider
    {
        private readonly IPublicationResolver PublicationResolver;
        protected readonly ILogger LoggerService;
        protected readonly IDD4TConfiguration Configuration;


        public BaseProvider(IProvidersCommonServices commonServices)
        {
            if (commonServices == null)
                throw new ArgumentNullException("commonServices");

            LoggerService = commonServices.Logger;
            PublicationResolver = commonServices.PublicationResolver;
            Configuration = commonServices.Configuration;

        }
        private int publicationId = 0;
        public int PublicationId
        {
            get
            {
                if (publicationId == 0)
                    return PublicationResolver.ResolvePublicationId();

                return publicationId;
            }
            set
            {
                publicationId = value;
            }
        }
    }
}
