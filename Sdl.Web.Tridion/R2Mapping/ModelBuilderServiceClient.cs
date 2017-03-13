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
using System.Collections.Generic;
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
        private static readonly string ExtensionProperty = "dxa-model-service";
        private static readonly string PreviewSessionTokenHeader = "x-preview-session-token";
        private static readonly string PreviewSessionTokenCookie = "preview-session-token";

        private readonly Uri _modelBuilderService;
        private readonly IOAuthTokenProvider _tokenProvider;

        public ModelBuilderServiceClient()
        {
            _modelBuilderService = GetModelBuilderServiceEndpoint();
            Log.Debug($"DXA Model Builder Service: {_modelBuilderService}.");
            _tokenProvider = DiscoveryServiceProvider.DefaultTokenProvider;
        }

        public PageModelData GetPageModelData(string urlPath, Localization localization, bool addIncludes)      
            => LoadData<PageModelData>(CreatePageModelRequestUri(urlPath, localization, addIncludes));

        public EntityModelData GetEntityModelData(string id, Localization localization)
           => LoadData<EntityModelData>(CreateEntityModelRequestUri(id, localization));

        private Uri CreatePageModelRequestUri(string urlPath, Localization localization, bool addIncludes)
            => new Uri(_modelBuilderService, $"/PageModel/tcm/{localization.Id}{GetCanonicalUrlPath(urlPath)}?includes={addIncludes}");

        private Uri CreatePageModelRequestUri(string urlPath, Localization localization)
            => new Uri(_modelBuilderService, $"/PageModel/tcm/{localization.Id}{GetCanonicalUrlPath(urlPath)}");
        
        private Uri CreateEntityModelRequestUri(string tcmId, Localization localization)
            => new Uri(_modelBuilderService, $"/EntityModel/tcm/{localization.Id}/{tcmId}");

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
                }
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
            var modelBuilderService = contentService.ExtensionProperties.Single(x => x.Key.Equals(ExtensionProperty, StringComparison.OrdinalIgnoreCase));
            return modelBuilderService != null ? new Uri(modelBuilderService.Value) : null;
        }
    }
}
