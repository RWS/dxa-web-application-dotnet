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
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Logging;

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
            Log.Debug($"DXA Model Builder Service: {_modelBuilderService}.");
            _tokenProvider = DiscoveryServiceProvider.DefaultTokenProvider;
        }

        public PageModelData GetPageModelData(string urlPath, Localization localization, bool addIncludes = true)
        {
            urlPath = GetCanonicalUrlPath(urlPath);
            return LoadData<PageModelData>(CreatePageModelRequestUri(urlPath, localization));
        }

        public EntityModelData GetEntityModelData(string id, Localization localization)
        {
            return LoadData<EntityModelData>(CreateEntityModelRequestUri(id, localization));
        }

        public T GetViewModel<T>(string urlPath, Localization localization) where T : ViewModel
        {
            //TODO
            return default(T);
        }

        private Uri CreatePageModelRequestUri(string urlPath, Localization localization)
        {
            return new Uri(_modelBuilderService, $"/PageModel/tcm/{localization.Id}{urlPath}");
        }

        private Uri CreateEntityModelRequestUri(string tcmId, Localization localization)
        {
            return new Uri(_modelBuilderService, $"/EntityModel/tcm/{localization.Id}/{tcmId}");
        }

        private T LoadData<T>(Uri requestUri)
        {
            try
            {
                string content = ProcessRequest(requestUri);
                if (content == null)
                {
                    throw new DxaItemNotFoundException($"Failed to load model for '{requestUri}' from model builder service.");
                }
                return JsonConvert.DeserializeObject<T>(content, DataModelBinder.SerializerSettings);
            }
            catch(Exception e)
            {
                Log.Error($"Failed to load data from model service at '{requestUri}'. Exception {e.Message} occured.");
                throw new DxaItemNotFoundException($"Failed to load model for '{requestUri}' from model builder service.");
            }           
        }

        private string ProcessRequest(Uri requestUri)
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
