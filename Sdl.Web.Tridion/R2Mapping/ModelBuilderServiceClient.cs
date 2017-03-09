using Sdl.Web.Delivery.DiscoveryService;
using System;
using System.Linq;
using Sdl.Web.Common.Configuration;
using System.Net;
using System.IO;
using Sdl.Web.DataModel;
using Newtonsoft.Json;
using Sdl.Web.Delivery.Service;
using Sdl.Web.Common;

namespace Sdl.Web.Tridion.R2Mapping
{
    /// <summary>
    /// Model Builder Service Client
    /// </summary>
    public class ModelBuilderServiceClient
    {
        private readonly Uri _modelBuilderService;
        private readonly IOAuthTokenProvider _tokenProvider;

        public ModelBuilderServiceClient()
        {
            _modelBuilderService = GetModelBuilderServiceEndpoint();
            _tokenProvider = DiscoveryServiceProvider.DefaultTokenProvider;
        }

        public PageModelData GetPageModel(string urlPath, Localization localization, bool addIncludes = true)
        {
            urlPath = GetCanonicalUrlPath(urlPath);

            string pageContent = GetPageContent(urlPath, localization);
            if (pageContent != null)
            {
                return JsonConvert.DeserializeObject<PageModelData>(pageContent, DataModelBinder.SerializerSettings);
            }
            return null;
        }

        public EntityModelData GetEntityModel(string id, Localization localization)
        {
            string entityContent = ProcessRequest(CreateEntityModelRequestUri(id, localization));
            if (entityContent != null)
            {
                return JsonConvert.DeserializeObject<EntityModelData>(entityContent, DataModelBinder.SerializerSettings);
            }
            return null;
        }      

        public string GetPageContent(string urlPath, Localization localization)
        {
            return ProcessRequest(CreatePageModelRequestUri(urlPath, localization));
        }

        private Uri CreatePageModelRequestUri(string urlPath, Localization localization)
        {
            return new Uri(_modelBuilderService, $"/PageModel/tcm/{localization.Id}{urlPath}");
        }

        private Uri CreateEntityModelRequestUri(string tcmId, Localization localization)
        {
            return new Uri(_modelBuilderService, $"/EntityModel/tcm/{localization.Id}/{tcmId}");
        }

        private string ProcessRequest(Uri requestUri)
        {
            try
            {                
                WebRequest request = WebRequest.Create(requestUri);
                request.ContentType = "application/json; charset=utf-8";
                if (_tokenProvider != null)
                {
                    request.Headers.Add(_tokenProvider.AuthRequestHeaderName, _tokenProvider.AuthRequestHeaderValue);
                }
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        if (responseStream != null)
                        {
                            using (StreamReader streamReader = new StreamReader(responseStream))
                            {
                                return streamReader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch
            {
                // todo: need to handle any exceptions and log things here
            }
            return null;
        }

        private string GetCanonicalUrlPath(string urlPath)
        {
            string result = urlPath ?? Constants.IndexPageUrlSuffix;
            if (!result.StartsWith("/"))
            {
                result = "/" + result;
            }
            if (result.EndsWith("/"))
            {
                result += Constants.DefaultExtensionLessPageName;
            }
            else if (result.EndsWith(Constants.DefaultExtension))
            {
                result = result.Substring(0, result.Length - Constants.DefaultExtension.Length);
            }
            return result;
        }

        private Uri GetModelBuilderServiceEndpoint()
        {
            IDiscoveryService service = DiscoveryServiceProvider.Instance.ServiceClient;
            var contentService = service.CreateQuery<Delivery.DiscoveryService.Tridion.WebDelivery.Platform.ContentServiceCapability>().Take(1).FirstOrDefault();
            var modelBuilderService = contentService.ExtensionProperties.Single(x => x.Key.Equals("dxa-model-service", StringComparison.OrdinalIgnoreCase));
            return modelBuilderService != null ? new Uri(modelBuilderService.Value) : null;
        }
    }
}
