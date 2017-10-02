using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Contracts.Resolvers;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.ContentModel.Contracts.Configuration;
using System;

namespace DD4T.Providers.SDLWeb85.CIL
{
    public class BaseProvider : IProvider
    {
      
        private readonly IPublicationResolver PublicationResolver;
        protected readonly ILogger LoggerService;
        protected readonly IDD4TConfiguration Configuration;
         

        public BaseProvider(IProvidersCommonServices providersCommonServices)
        {
            if (providersCommonServices == null)
                throw new ArgumentNullException("providersCommonServices");

            LoggerService = providersCommonServices.Logger;
            PublicationResolver = providersCommonServices.PublicationResolver;
            Configuration = providersCommonServices.Configuration;

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
