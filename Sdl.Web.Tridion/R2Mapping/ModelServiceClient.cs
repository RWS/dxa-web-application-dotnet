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
using System.Web.Configuration;
using System.Threading.Tasks;

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

        private readonly IOAuthTokenProvider _tokenProvider;
        private readonly Uri _modelServiceBaseUri;
        private readonly int _serviceTimeout;
        private readonly int _serviceRetryCount;

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

            public override string ToString()
                => $"Status={Status}, Error='{Error}', Exception='{Exception}', Message='{Message}', Path='{Path}'";
        }

        public ModelServiceClient()
        {
            _modelServiceBaseUri = GetModelServiceUri();
            Log.Debug($"{ModelServiceName} found at URL '{_modelServiceBaseUri}'");
            _tokenProvider = DiscoveryServiceProvider.DefaultTokenProvider;
            int n;
            _serviceRetryCount = int.TryParse(
                WebConfigurationManager.AppSettings["model-builder-service-retries"], out n)
                ? n
                : 4; // default to 4 retry attempts

            _serviceTimeout = int.TryParse(
                WebConfigurationManager.AppSettings["model-builder-service-timeout"], out n)
                ? n
                : 100000; // default 100seconds
        }

        public PageModelData GetPageModelData(string urlPath, Localization localization, bool addIncludes)
            => LoadData<PageModelData>(CreatePageModelRequestUri(urlPath, localization, addIncludes));

        public EntityModelData GetEntityModelData(string id, Localization localization)
           => LoadData<EntityModelData>(CreateEntityModelRequestUri(id, localization));

        private Uri CreatePageModelRequestUri(string urlPath, Localization localization, bool addIncludes)
            => new Uri(_modelServiceBaseUri, $"/PageModel/{localization.CmUriScheme}/{localization.Id}{GetCanonicalUrlPath(urlPath)}?{GetIncludesParam(addIncludes)}");

        private static string GetIncludesParam(bool addIncludes)
            => "includes=" + (addIncludes ? "INCLUDE" : "EXCLUDE");

        private Uri CreateEntityModelRequestUri(string entityId, Localization localization)
            => new Uri(_modelServiceBaseUri, $"/EntityModel/{localization.CmUriScheme}/{localization.Id}/{entityId}");

        private T LoadData<T>(Uri requestUri) where T: ViewModelData
        {
            bool success;
            string responseBody = ProcessRequest(requestUri, out success);

            // Deserialize the response body (should be ViewModelData or ModelServiceError)
            ModelServiceError serviceError;
            try
            {
                if (success)
                {
                    return JsonConvert.DeserializeObject<T>(responseBody, DataModelBinder.SerializerSettings);
                }
                serviceError = JsonConvert.DeserializeObject<ModelServiceError>(responseBody);
            }
            catch (Exception ex)
            {
                const int maxCharactersToLog = 1000;
                if ((responseBody != null) && (responseBody.Length > maxCharactersToLog))
                {
                    responseBody = responseBody.Substring(0, maxCharactersToLog) + "...";
                }
                Log.Error("{0} returned an unexpected response for URL '{1}':\n{2} ", ModelServiceName, requestUri, responseBody);
                throw new DxaException($"{ModelServiceName} returned an unexpected response.", ex);
            }

            if (serviceError.Status == (int) HttpStatusCode.NotFound)
            {
                // Item not found; return null. The Content Provider will throw an DxaItemNotFoundException.
                return null;
            }

            Log.Error("{0} returned an error response: {1}", ModelServiceName, serviceError);
            throw new DxaException($"{ModelServiceName} returned an error: {serviceError.Message ?? serviceError.Error}");
        }

        private string ProcessRequest(Uri requestUri, out bool success)
        {
            using (new Tracer(requestUri))
            {
                Log.Debug($"Sending {ModelServiceName} Request: {requestUri}");

                HttpWebRequest request = (HttpWebRequest) WebRequest.Create(requestUri);
                request.Timeout = _serviceTimeout;
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
                    if ((headers != null) && headers.ContainsKey(PreviewSessionTokenHeader))
                    {
                        // See CRQ-3935
                        SetCookie(request, PreviewSessionTokenCookie, headers[PreviewSessionTokenHeader][0]);
                    }

                    Dictionary<string, string> cookies = claimStore.Get<Dictionary<string, string>>(new Uri(WebClaims.REQUEST_COOKIES));
                    if ((cookies != null) && cookies.ContainsKey(PreviewSessionTokenCookie))
                    {
                        SetCookie(request, PreviewSessionTokenCookie, cookies[PreviewSessionTokenCookie]);
                    }
                }

                int attempt = 0;
                do
                {
                    try
                    {
                        attempt++;
                        using (WebResponse response = request.GetResponse())
                        {
                            string responseBody = GetResponseBody(response);
                            success = true;
                            return responseBody;
                        }
                    }
                    catch (WebException ex)
                    {
                        if (ex.Status == WebExceptionStatus.Timeout && attempt < _serviceRetryCount)
                        {
                            Log.Debug("{0} timed out, attempting a retry request for URL '{1}'.", ModelServiceName,
                                requestUri);
                            Task.Delay(TimeSpan.FromMilliseconds(2000)).Wait();
                        }
                        else
                        {
                            if (ex.Status != WebExceptionStatus.ProtocolError)
                            {
                                throw new DxaException(
                                    $"{ModelServiceName} request for URL '{requestUri}' failed: {ex.Status}", ex);
                            }
                            success = false;
                            return GetResponseBody(ex.Response);
                        }
                    }
                } while (true);
            }
        }

        private static void SetCookie(HttpWebRequest httpWebRequest, string name, string value)
        {
            // Quick-and-dirty way: just directly set the "Cookie" HTTP header
            httpWebRequest.Headers.Add("Cookie", $"{name}={value}");
        }

        private static string GetResponseBody(WebResponse webResponse)
        {
            using (Stream responseStream = webResponse?.GetResponseStream())
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
                .FirstOrDefault(xp => xp.Key.Equals(ModelServiceExtensionPropertyName, StringComparison.OrdinalIgnoreCase));
            if (modelServiceExtensionProperty == null)
            {
                throw new DxaException($"{ModelServiceName} is not registered; no extension property called '{ModelServiceExtensionPropertyName}' found on Content Service Capability.");
            }
            return new Uri(modelServiceExtensionProperty.Value);
        }
    }
}
