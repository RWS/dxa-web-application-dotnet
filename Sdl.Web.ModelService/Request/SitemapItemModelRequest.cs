using System;
using System.Runtime.Serialization;

namespace Sdl.Web.ModelService.Request
{
    public class SitemapItemModelRequest : IModelServiceRequest
    {
        public int PublicationId { get; set; }

        public SerializationBinder Binder { get; set; }

        public Uri BuildRequestUri(ModelServiceClient modelService)
        {
            return
                UriCreator.FromUri(modelService.ModelServiceBaseUri).WithPath($"api/navigation/{PublicationId}").Build();
        }
    }
}
