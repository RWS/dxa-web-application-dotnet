using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Contracts.Resolvers;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.ContentModel.Contracts.Configuration;
using System;
using Sdl.Web.ModelService;

namespace DD4T.Providers.DxaModelService
{
    public class BaseProvider : IProvider
    {      
        private readonly IPublicationResolver PublicationResolver;
        protected readonly ILogger LoggerService;
        protected readonly IDD4TConfiguration Configuration;
        protected readonly ModelServiceClient ModelServiceClient;

        public BaseProvider(IProvidersCommonServices providersCommonServices)
        {
            if (providersCommonServices == null)
                throw new ArgumentNullException("providersCommonServices");

            LoggerService = providersCommonServices.Logger;
            PublicationResolver = providersCommonServices.PublicationResolver;
            Configuration = providersCommonServices.Configuration;
            ModelServiceClient = new ModelServiceClient();
        }

        private int _publicationId = 0;
        public int PublicationId
        {
            get
            {
                return _publicationId == 0 ? PublicationResolver.ResolvePublicationId() : _publicationId;
            }
            set
            {
                _publicationId = value;
            }
        }
    }
}
