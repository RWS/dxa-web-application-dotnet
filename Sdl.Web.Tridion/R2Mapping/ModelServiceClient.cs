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
using Sdl.Web.Common.Logging;
using System.Collections.Generic;
using Sdl.Web.Delivery.DiscoveryService.Tridion.WebDelivery.Platform;
using Sdl.Web.Delivery.ServicesCore.ClaimStore;

namespace Sdl.Web.Tridion.R2Mapping
{
    /// <summary>
    /// Client for the DXA Model Service
    /// </summary>
    public class ModelServiceClient
    {
        private const string ModelServiceName = "DXA Model Service";
        private const string ModelServiceExtensionPropertyName = "dxa-model-service";
        private const string PreviewSessionTokenHeader = "x-preview-session-token";
        private const string PreviewSessionTokenCookie = "preview-session-token";

        private readonly Uri _modelServiceBaseUri;
        private readonly IOAuthTokenProvider _tokenProvider;

        private class ModelServiceError
        {
            [JsonProperty("status")]
            public int Status { get; set; }

            [JsonProperty("error")]
            public string Error { get; set; }

            [JsonProperty("exception")]
            public string Exception { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("path")]
            public string Path { get; set; }
        }

        public ModelServiceClient()
        {
            _modelServiceBaseUri = GetModelServiceUri();
            Log.Debug($"{ModelServiceName} found at URL '{_modelServiceBaseUri}'");
            _tokenProvider = DiscoveryServiceProvider.DefaultTokenProvider;
        }

        public PageModelData GetPageModelData(string urlPath, Localization localization, bool addIncludes)
            => LoadData<PageModelData>(CreatePageModelRequestUri(urlPath, localization, addIncludes));

        public EntityModelData GetEntityModelData(string id, Localization localization)
           => LoadData<EntityModelData>(CreateEntityModelRequestUri(id, localization));

        private Uri CreatePageModelRequestUri(string urlPath, Localization localization, bool addIncludes)
            => new Uri(_modelServiceBaseUri, $"/PageModel/{localization.CmUriScheme}/{localization.Id}{GetCanonicalUrlPath(urlPath)}?{GetIncludesParam(addIncludes)}");

        private static string GetIncludesParam(bool addIncludes)
            => "includes=" + (addIncludes ? "INCLUDE" : "EXCLUDE");

        private Uri CreateEntityModelRequestUri(string tcmId, Localization localization)
            => new Uri(_modelServiceBaseUri, $"/EntityModel/{localization.CmUriScheme}/{localization.Id}/{tcmId}");


        private T LoadData<T>(Uri requestUri) where T: ViewModelData
        {
            WebExceptionStatus status;
            string responseBody = ProcessRequest(requestUri, out status);
            if (status == WebExceptionStatus.Success)
            {
                return JsonConvert.DeserializeObject<T>(responseBody, DataModelBinder.SerializerSettings);
            }

            ModelServiceError serviceError;
            try
            {
                serviceError = JsonConvert.DeserializeObject<ModelServiceError>(responseBody);
            }
            catch (Exception)
            {
                throw new DxaException($"{ModelServiceName} returned an unexpected response: {responseBody}");
            }

            if (serviceError.Status == (int) HttpStatusCode.NotFound)
            {
                return null;
            }

            Log.Debug($"{ModelServiceName} returned a '{serviceError.Error ?? serviceError.Status.ToString()}' error for request URL '{requestUri}'");
            throw new DxaException($"{ModelServiceName} returned an error: {serviceError.Message ?? serviceError.Status.ToString()}");
        }

        private string ProcessRequest(Uri requestUri, out WebExceptionStatus status)
        {
            Log.Debug($"Sending {ModelServiceName} Request: {requestUri}");

            WebRequest request = WebRequest.Create(requestUri);
            request.ContentType = "application/json; charset=utf-8";
            // handle OAuth if available/required
            if (_tokenProvider != null)
            {
                request.Headers.Add(_tokenProvider.AuthRequestHeaderName, _tokenProvider.AuthRequestHeaderValue);
            }

            // forward on session preview cookie/header if available in ADF claimstore
            IClaimStore claimStore = AmbientDataContext.CurrentClaimStore;
            if (claimStore != null)
            {
                Dictionary<string, string[]> headers = claimStore.Get<Dictionary<string, string[]>>(new Uri(WebClaims.REQUEST_HEADERS));
                if (headers != null && headers.ContainsKey(PreviewSessionTokenHeader))
                {
                    request.Headers.Add(PreviewSessionTokenHeader, headers[PreviewSessionTokenHeader][0]);
                }

                Dictionary<string, string> cookies = claimStore.Get<Dictionary<string, string>>(new Uri(WebClaims.REQUEST_COOKIES));
                if (cookies != null && cookies.ContainsKey(PreviewSessionTokenCookie))
                {
                    string cookie = request.Headers["cookie"];
                    if (!string.IsNullOrEmpty(cookie))
                        cookie += ";";
                    cookie += cookies[PreviewSessionTokenCookie];
                    request.Headers["cookie"] = cookie;
                }
            }

            try
            {
                using (WebResponse response = request.GetResponse())
                {
                    string responseBody = GetResponseBody(response);
                    status = WebExceptionStatus.Success;
                    return responseBody;
                }
            }
            catch (WebException ex)
            {
                status = ex.Status;
                return GetResponseBody(ex.Response);
            }
        }

        private static string GetResponseBody(WebResponse webResponse)
        {
            using (Stream responseStream = webResponse.GetResponseStream())
            {
                if (responseStream == null)
                {
                    return null;
                }
                using (StreamReader streamReader = new StreamReader(responseStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        private static string GetCanonicalUrlPath(string urlPath)
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

        private static Uri GetModelServiceUri()
        {
            IDiscoveryService discoveryService = DiscoveryServiceProvider.Instance.ServiceClient;
            ContentServiceCapability contentService = discoveryService.CreateQuery<ContentServiceCapability>().Take(1).FirstOrDefault();
            if (contentService == null)
            {
                throw new DxaException("Content Service Capability not found in Discovery Service.");
            }
            ContentKeyValuePair modelServiceExtensionProperty = contentService.ExtensionProperties
                .Take(1).FirstOrDefault(xp => xp.Key.Equals(ModelServiceExtensionPropertyName, StringComparison.OrdinalIgnoreCase));
            if (modelServiceExtensionProperty == null)
            {
                throw new DxaException($"{ModelServiceName} is not registered; no extension property called '{ModelServiceExtensionPropertyName}' found on Content Service Capability.");
            }
            return new Uri(modelServiceExtensionProperty.Value);
        }
    }
}
