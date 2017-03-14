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
using Tridion.ContentDelivery.DynamicContent;
using Tridion.ContentDelivery.DynamicContent.Query;

namespace Sdl.Web.Tridion.R2Mapping
{
    /// <summary>
    /// Model Builder Service Client
    /// </summary>
    public class ModelBuilderServiceClient
    {
        private const string DxaModelServiceExtensionProperty = "dxa-model-service";
        private const string PreviewSessionTokenHeader = "x-preview-session-token";
        private const string PreviewSessionTokenCookie = "preview-session-token";

        private readonly Uri _modelBuilderService;
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

        public ModelBuilderServiceClient()
        {
            _modelBuilderService = GetModelBuilderServiceEndpoint();
            Log.Debug($"DXA Model Builder Service: {_modelBuilderService}.");
            _tokenProvider = DiscoveryServiceProvider.DefaultTokenProvider;
        }

        public PageModelData GetPageModelData(string urlPath, Localization localization, bool addIncludes)
            => LoadData<PageModelData>(CreatePageModelRequestUri(urlPath, localization, addIncludes), urlPath);

        public EntityModelData GetEntityModelData(string id, Localization localization)
           => LoadData<EntityModelData>(CreateEntityModelRequestUri(id, localization), id);

        private Uri CreatePageModelRequestUri(string urlPath, Localization localization, bool addIncludes)
            => new Uri(_modelBuilderService, $"/PageModel/{localization.CmUriScheme}/{localization.Id}{GetCanonicalUrlPath(urlPath)}?{GetIncludesParam(addIncludes)}");

        private static string GetIncludesParam(bool addIncludes)
            => "includes=" + (addIncludes ? "INCLUDE" : "EXCLUDE");

        private Uri CreateEntityModelRequestUri(string tcmId, Localization localization)
            => new Uri(_modelBuilderService, $"/EntityModel/{localization.CmUriScheme}/{localization.Id}/{tcmId}");

        public string GetRawPageData(string urlPath, Localization localization)
        {
            // todo: this does not use the model service but goes directly though the CIL.
            //       provide this functionality on the model service?
            using (new Tracer(urlPath, localization))
            {
                if (!urlPath.EndsWith(Constants.DefaultExtension) && !urlPath.EndsWith(".json"))
                {
                    urlPath += Constants.DefaultExtension;
                }
                string escapedUrlPath = Uri.EscapeUriString(urlPath);
                global::Tridion.ContentDelivery.DynamicContent.Query.Query brokerQuery = new global::Tridion.ContentDelivery.DynamicContent.Query.Query
                {
                    Criteria = CriteriaFactory.And(new Criteria[]
                    {
                        new PageURLCriteria(escapedUrlPath),
                        new PublicationCriteria(Convert.ToInt32(localization.Id)),
                        new ItemTypeCriteria(64)
                    })
                };

                string[] pageUris = brokerQuery.ExecuteQuery();
                if (pageUris.Length == 0)
                {
                    return null;
                }
                if (pageUris.Length > 1)
                {
                    throw new DxaException($"Broker Query for Page URL path '{urlPath}' in Publication '{localization.Id}' returned {pageUris.Length} results.");
                }

                PageContentAssembler pageContentAssembler = new PageContentAssembler();
                return pageContentAssembler.GetContent(pageUris[0]);
            }
        }

        private T LoadData<T>(Uri requestUri, string itemId)
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
                throw new DxaException($"DXA Model Service returned an unexpected response: {responseBody}");
            }

            if (serviceError.Status == (int) HttpStatusCode.NotFound)
            {
                throw new DxaItemNotFoundException(itemId);
            }

            Log.Debug($"DXA Model service returned a '{serviceError.Error ?? serviceError.Status.ToString()}' error for request URL '{requestUri}'");
            throw new DxaException($"DXA Model Service returned an error: {serviceError.Message ?? serviceError.Status.ToString()}");
        }

        private string ProcessRequest(Uri requestUri, out WebExceptionStatus status)
        {
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

        private static Uri GetModelBuilderServiceEndpoint()
        {
            IDiscoveryService service = DiscoveryServiceProvider.Instance.ServiceClient;
            ContentServiceCapability contentService = service.CreateQuery<ContentServiceCapability>().Take(1).FirstOrDefault();
            if (contentService == null)
            {
                throw new DxaException("Content Service Capability not found in Discovery Service.");
            }
            ContentKeyValuePair dxaModelServiceExtensionProperty = contentService.ExtensionProperties
                .Take(1).FirstOrDefault(xp => xp.Key.Equals(DxaModelServiceExtensionProperty, StringComparison.OrdinalIgnoreCase));
            if (dxaModelServiceExtensionProperty == null)
            {
                throw new DxaException($"DXA Model Service is not registered; no extension property called '{DxaModelServiceExtensionProperty}' found on Content Service Capability.");
            }
            return new Uri(dxaModelServiceExtensionProperty.Value);
        }
    }
}
