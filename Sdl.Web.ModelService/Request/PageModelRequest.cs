using System;
using System.Runtime.Serialization;

namespace Sdl.Web.ModelService.Request
{   
    public enum PageInclusion
    {       
        INCLUDE, // Page regions should be included.        
        EXCLUDE  // Page regions should be excluded.
    }

    public class PageModelRequest : IModelServiceRequest
    {
        private const string DefaultExtensionLessPageName = "index";
        private const string DefaultExtension = ".html";
        private const string IndexPageUrlSuffix = "/" + DefaultExtensionLessPageName;
        
        public int PublicationId { get; set; }

        public string CmUriScheme { get; set; } = "tcm";

        public string Path { get; set; }

        public string ItemId { get; set; }

        public ContentType ContentType { get; set; } = ContentType.MODEL;  

        public DataModelType DataModelType { get; set; } = DataModelType.R2;

        public PageInclusion PageInclusion { get; set; } = PageInclusion.INCLUDE;

        public SerializationBinder Binder { get; set; }

        public Uri BuildRequestUri(ModelServiceClient modelService)
        {
            return UriCreator.FromUri(modelService.ModelServiceBaseUri)
                    .WithPath($"PageModel/{CmUriScheme}/{PublicationId}/{GetCanonicalUrlPath(Path)}")
                    .WithQueryParam("includes", PageInclusion)
                    .WithQueryParam("modelType", DataModelType)
                    .WithQueryParam("raw", ContentType == ContentType.RAW)
                    .Build();
        }

        private static string GetCanonicalUrlPath(string urlPath)
        {
            string result = urlPath ?? IndexPageUrlSuffix;
            result = result.TrimStart('/');
            if (result.EndsWith("/"))
            {
                result += DefaultExtensionLessPageName;
            }
            else if (result.EndsWith(DefaultExtension))
            {
                result = result.Substring(0, result.Length - DefaultExtension.Length);
            }
            return result;
        }
    }
}
