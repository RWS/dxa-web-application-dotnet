using Sdl.Web.Delivery.DiscoveryService;
using System;
using System.Linq;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Sdl.Web.Delivery.Service;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Sdl.Web.Delivery.DiscoveryService.Tridion.WebDelivery.Platform;
using Sdl.Web.Delivery.ServicesCore.ClaimStore;
using System.Threading.Tasks;
using Sdl.Web.Delivery.Core;
using Sdl.Web.ModelService.Request;

namespace Sdl.Web.ModelService
{
    /// <summary>
    /// Client for the DXA Model Service
    /// </summary>
    public class ModelServiceClient
    {
        #region Fields
        // if something goes wrong when getting the model-service endpoint we just default to this so the
        // dxa web application continues to run up and initialize but you clearly spot something is wrong! 
        // Although the model building pipeline will fail it will not prevent dxa from initializing and the 
        // the next request will attempt to get the model-service endpoint again. Initial loading of dxa is
        // time consuming and most of the work done is not dependent on a model-service so we don't waste this
        // work.
        private static readonly Uri DefaultModelServiceUri = new Uri("http://dxa.model.service.uri.missing");
        private const string ModelServiceName = "DXA Model Service";
        private const string ModelServiceExtensionPropertyName = "dxa-model-service";
        private const string PreviewSessionTokenHeader = "x-preview-session-token";
        private const string PreviewSessionTokenCookie = "preview-session-token";
        private readonly IOAuthTokenProvider _tokenProvider;
        private Uri _modelServiceUri;
        #endregion

        #region ModelServiceError

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

        #endregion

        public ModelServiceClient()
        {
            try
            {
                ModelServiceBaseUri = GetModelServiceUri(null); // force discovery service lookup
            }
            catch (Exception)
            {
                // something went wrong with discovery lookup of model-service. we could continue
                // trying each time the model-service is requested
                ModelServiceBaseUri = null;
                throw;
            }            
            _tokenProvider = DiscoveryServiceProvider.DefaultTokenProvider;
        }

        public ModelServiceClient(string modelServiceUri)
        {
            try
            {
                ModelServiceBaseUri = GetModelServiceUri(modelServiceUri);
            }
            catch (Exception e)
            {
                Logger.Error("Error initializing ModelServiceClient.", e);
            }
            _tokenProvider = DiscoveryServiceProvider.DefaultTokenProvider;
        }

        public ModelServiceClient(string modelServiceUri, int retryCount, int timeout)
            : this(modelServiceUri)
        {
            RetryCount = retryCount;
            Timeout = timeout;
        }

        public Uri ModelServiceBaseUri
        {
            private set { _modelServiceUri = value; }
            get
            {
                if (_modelServiceUri != null) return _modelServiceUri;
                try
                {
                    // Attempt to get model-service endpoint. May fail for a number of reasons:
                    // 1. discovery-service not running
                    // 2. content-service capability/extension property for model-service not configured
                    // 3. uri configured on extension property is malformed.
                    _modelServiceUri = GetModelServiceUri(null);
                }
                catch (Exception e)
                {                   
                    // Log failure when getting the model-service endpoint.
                    Logger.Error("Failed to get model-service endpoint from discovery-service.", e);
                }
                // Return either the endpoint (if we have one) or our default.
                return _modelServiceUri ?? DefaultModelServiceUri;
            }
        }

        public int Timeout { get; set; } = 10000;

        public int RetryCount { get; set; } = 4;

        public JsonSerializerSettings JsonSettings(SerializationBinder binder) => new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Binder = binder,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        /// <summary>
        /// Perform Model Service request
        /// </summary>
        public ModelServiceResponse<string> PerformRequest(IModelServiceRequest request)
        {
            return PerformRequest<string>(request);
        }

        /// <summary>
        /// Perform Model Service request
        /// </summary>
        public ModelServiceResponse<T> PerformRequest<T>(IModelServiceRequest request)
        {
            Uri requestUri = request.BuildRequestUri(this);
            bool success = true;
            uint hashcode = 0;
            string responseBody = null;
            try
            {
                responseBody = PerformRequest(requestUri);
                hashcode = Murmur3.Hash(responseBody);
            }
            catch (ModelServiceRequestException e)
            {
                success = false;
                responseBody = e.ResponseBody;
            }
            catch
            {
                success = false;
            }

            ModelServiceError serviceError;
            try
            {
                if (success)
                {
                    T responseObject;
                    if (typeof (T) == typeof (string))
                        responseObject = (T) Convert.ChangeType(responseBody, typeof (T));
                    else
                        responseObject = JsonConvert.DeserializeObject<T>(responseBody, JsonSettings(request.Binder));
                    return ModelServiceResponse<T>.Create(responseObject, hashcode);
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
                throw new ModelServiceException(
                    $"{ModelServiceName} returned an unexpected response from request '{requestUri}' of {responseBody}.", ex);
            }

            if (serviceError == null || serviceError.Status == (int) HttpStatusCode.NotFound)
            {
                throw new ItemNotFoundException(
                    $"{ModelServiceName} failed to locate item from request '{requestUri}'.");
            }

            throw new ModelServiceException(
                $"{ModelServiceName} returned an error: {serviceError.Message ?? serviceError.Error}");
        }

        private string PerformRequest(Uri requestUri)
        {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(requestUri);
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = Timeout;
            request.ContentType = "application/json; charset=utf-8";
            request.UserAgent = "Microsoft ADO.NET Data Services";

            // handle OAuth if available/required
            if (_tokenProvider != null)
            {
                request.Headers.Add(_tokenProvider.AuthRequestHeaderName,
                    _tokenProvider.AuthRequestHeaderValue);
            }

            // forward on session preview cookie/header if available in ADF claimstore
            IClaimStore claimStore = AmbientDataContext.CurrentClaimStore;
            if (claimStore != null)
            {
                Dictionary<string, string[]> headers =
                    claimStore.Get<Dictionary<string, string[]>>(new Uri(WebClaims.REQUEST_HEADERS));
                if ((headers != null) && headers.ContainsKey(PreviewSessionTokenHeader))
                {
                    // See CRQ-3935
                    SetCookie(request, PreviewSessionTokenCookie, headers[PreviewSessionTokenHeader][0]);
                }

                Dictionary<string, string> cookies =
                    claimStore.Get<Dictionary<string, string>>(new Uri(WebClaims.REQUEST_COOKIES));
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
                        return GetResponseBody(response);                        
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.Timeout && attempt < RetryCount)
                    {
                        Task.Delay(TimeSpan.FromMilliseconds(2000)).Wait();
                    }
                    else
                    {
                        if (ex.Status != WebExceptionStatus.ProtocolError)
                        {
                            throw new ModelServiceException(
                                $"{ModelServiceName} request for URL '{requestUri}' failed: {ex.Status}", ex);
                        }
                        throw new ModelServiceRequestException(GetResponseBody(ex.Response));
                    }
                }
            } while (true);
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

        private static Uri GetModelServiceUri(string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                IDiscoveryService discoveryService = DiscoveryServiceProvider.Instance.ServiceClient;
                ContentServiceCapability contentService =
                    discoveryService.CreateQuery<ContentServiceCapability>().Take(1).FirstOrDefault();
                if (contentService == null)
                {
                    throw new ModelServiceException("Content Service Capability not found in Discovery Service.");
                }
                ContentKeyValuePair modelServiceExtensionProperty = contentService.ExtensionProperties
                    .FirstOrDefault(
                        xp => xp.Key.Equals(ModelServiceExtensionPropertyName, StringComparison.OrdinalIgnoreCase));
                if (modelServiceExtensionProperty == null)
                {
                    throw new ModelServiceException(
                        $"{ModelServiceName} is not registered; no extension property called '{ModelServiceExtensionPropertyName}' found on Content Service Capability.");
                }
                uri = modelServiceExtensionProperty.Value ?? string.Empty;
            }
            uri = uri.TrimEnd('/') + '/';
            Uri baseUri;          
            if (!Uri.TryCreate(uri, UriKind.Absolute, out baseUri))
            {
                throw new ModelServiceException($"{ModelServiceName} is using an invalid uri '{uri}'.");
            }
            Logger.Debug($"{ModelServiceName} found at URL '{baseUri}'.");
            return baseUri;
        }
    }
}
