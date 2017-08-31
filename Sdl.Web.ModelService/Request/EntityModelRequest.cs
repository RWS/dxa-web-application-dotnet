using System;
using System.Runtime.Serialization;

namespace Sdl.Web.ModelService.Request
{    
    // Strategy of DCP template resolving is template ID is missing in request.
    public enum DcpType
    {
        DEFAULT, // If template is not set, then load a default DXA Data Presentation.         
        HIGHEST_PRIORITY // If template is not set, then load a Component Presentation with the highest priority.
    }

    public class EntityModelRequest : IModelServiceRequest
    {                
        public int PublicationId { get; set; }

        public int ComponentId { get; set; }

        public int? TemplateId { get; set; }
        
        public string EntityId
        {
            get
            {
                return $"{ComponentId}" + (TemplateId.HasValue ? $"-{TemplateId.Value}" : string.Empty);
            }
            set
            {
                string[] parts = value?.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts == null) return;
                int id;
                if (int.TryParse(parts[0], out id))
                {
                    ComponentId = id;
                }

                if (parts.Length > 1 && int.TryParse(parts[1], out id))
                {
                    TemplateId = id;
                }
            }
        }

        public string CmUriScheme { get; set; } = "tcm";

        public ContentType ContentType { get; set; }      

        public DataModelType DataModelType { get; set; } = DataModelType.R2;

        public DcpType DcpType { get; set; } = DcpType.DEFAULT;

        public SerializationBinder Binder { get; set; }

        public Uri BuildRequestUri(ModelServiceClient modelService)
        {
            return UriCreator.FromUri(modelService.ModelServiceBaseUri)
                   .WithPath($"EntityModel/{CmUriScheme}/{PublicationId}/{EntityId}")
                   .WithQueryParam("modelType", DataModelType)
                   .WithQueryParam("dcpType", DcpType)
                   .WithQueryParam("raw", ContentType == ContentType.RAW)
                   .Build();
        }      
    }
}
