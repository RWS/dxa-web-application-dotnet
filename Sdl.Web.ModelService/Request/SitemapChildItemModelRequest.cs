using System;
using System.Runtime.Serialization;

namespace Sdl.Web.ModelService.Request
{
    public class SitemapChildItemModelRequest : IModelServiceRequest
    {
        public int PublicationId { get; set; }

        public string ParentSitemapItemId { get; set; }

        public bool IncludeAncestors { get; set; }

        public int DescendantLevels { get; set; }

        public SerializationBinder Binder { get; set; }

        public Uri BuildRequestUri(ModelServiceClient modelService)
        {
            return UriCreator.FromUri(modelService.ModelServiceBaseUri)
                  .WithPath($"api/navigation/{PublicationId}/subtree/{ParentSitemapItemId}")
                  .WithQueryParam("includeAncestors", IncludeAncestors)
                  .WithQueryParam("descendantLevels", DescendantLevels).Build();
        }
    }
}
