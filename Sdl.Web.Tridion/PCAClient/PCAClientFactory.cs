using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Delivery.DiscoveryService;
using Sdl.Web.Delivery.ServicesCore.ClaimStore;
using Sdl.Web.HttpClient.Auth;
using Sdl.Web.IQQuery.API;
using Sdl.Web.IQQuery.Client;
using Sdl.Web.PublicContentApi;
using Sdl.Web.PublicContentApi.ContentModel;

namespace Sdl.Web.Tridion.PCAClient
{
    /// <summary>
    /// Public Content Api Factory creates PCA clients with context claim forwarding and
    /// OAuthentication.
    /// </summary>
    public sealed class PCAClientFactory
    {
        private readonly Uri _endpoint;
        private readonly Uri _iqEndpoint;
        private readonly string _iqSearchIndex;
        private readonly bool _claimForwarding = true;
        private readonly IAuthentication _oauth;
        private const string PreviewSessionTokenHeader = "x-preview-session-token";
        private const string PreviewSessionTokenCookie = "preview-session-token";

        private static readonly Lazy<PCAClientFactory> lazy =
            new Lazy<PCAClientFactory>(() => new PCAClientFactory());

        public static PCAClientFactory Instance => lazy.Value;
        
        private PCAClientFactory()
        {
            try
            {
                var setting = WebConfigurationManager.AppSettings["pca-claim-forwarding"];
                if (!string.IsNullOrEmpty(setting))
                {
                    _claimForwarding = setting.Equals("true", StringComparison.InvariantCultureIgnoreCase);
                }
                string uri = WebConfigurationManager.AppSettings["pca-service-uri"];
                if (string.IsNullOrEmpty(uri))
                {
                    IDiscoveryService discoveryService = DiscoveryServiceProvider.Instance.ServiceClient;
                    Uri contentServiceUri = discoveryService.ContentServiceUri;
                    if (contentServiceUri == null)
                    {
                        Log.Error("Unable to retrieve content-service endpoint from discovery-service.");
                    }
                    else
                    {
                        Log.Info($"Content-service endpoint located at {contentServiceUri}");
                        _endpoint = new Uri(contentServiceUri.AbsoluteUri.Replace("content.svc",
                            "cd/api"));
                    }
                }
                else
                {
                    _endpoint = new Uri(uri);
                }
                if (_endpoint == null)
                {
                    throw new PCAClientException("Unable to retrieve endpoint for Public Content Api");
                }

                uri = WebConfigurationManager.AppSettings["iq-service-uri"];
                _iqSearchIndex = WebConfigurationManager.AppSettings["iq-search-index"];
                if (string.IsNullOrEmpty(uri))
                {
                    IDiscoveryService discoveryService = DiscoveryServiceProvider.Instance.ServiceClient;
                    _iqEndpoint = discoveryService.IQServiceUri;
                }
                else
                {
                    _iqEndpoint = new Uri(uri);
                }

                _oauth = new OAuth(DiscoveryServiceProvider.DefaultTokenProvider);
                Log.Debug($"PCAClient found at URL '{_endpoint}' with claim forwarding = {_claimForwarding}");
                Log.Info(_iqEndpoint == null
                    ? "Unable to retrieve endpoint for IQ Search Service."
                    : $"IQSearch found at URL '{_iqEndpoint}'.");
                if (!string.IsNullOrEmpty(_iqSearchIndex))
                {
                    Log.Info($"IQ Search Index = {_iqSearchIndex}");
                }
                else
                {
                    Log.Warn(
                        "No IQ Search Index configured, using default udp-index. Please add the appSetting iq-search-index and set to your search index name.");
                }
            }
            catch (Exception ex)
            {
                const string error = "Failed to initialize PCA client. Check the UDP services are running.";
                Log.Error(ex, error);
                throw;
            }
        }

        /// <summary>
        /// Returns a fully constructed IQ Search client
        /// </summary>
        /// <typeparam name="TSearchResultSet">Type used for result set</typeparam>
        /// <typeparam name="TSearchResult">Type ised for result</typeparam>
        /// <returns>IQ Search Client</returns>
        public IQSearchClient<TSearchResultSet, TSearchResult> CreateSearchClient<TSearchResultSet, TSearchResult>()
            where TSearchResultSet : IQueryResultData<TSearchResult> where TSearchResult : IQueryResult => new IQSearchClient<TSearchResultSet, TSearchResult>(_iqEndpoint, _oauth, _iqSearchIndex);

        /// <summary>
        /// Returns a fully constructed IQ Search client
        /// </summary>
        /// <typeparam name="TSearchResultSet">Type used for result set</typeparam>
        /// <typeparam name="TSearchResult">Type ised for result</typeparam>
        /// <param name="searchIndex">Search Index</param>
        /// <returns>IQ Search Client</returns>
        public IQSearchClient<TSearchResultSet, TSearchResult> CreateSearchClient<TSearchResultSet, TSearchResult>(string searchIndex)
            where TSearchResultSet : IQueryResultData<TSearchResult> where TSearchResult : IQueryResult => new IQSearchClient<TSearchResultSet, TSearchResult>(_iqEndpoint, _oauth, searchIndex);

        /// <summary>
        /// Returns a fully constructed IQ Search client
        /// </summary>
        /// <typeparam name="TSearchResultSet">Type used for result set</typeparam>
        /// <typeparam name="TSearchResult">Type ised for result</typeparam>
        /// <param name="endpoint">IQ Search endpoint</param>
        /// <param name="searchIndex">Search Index</param>
        /// <returns></returns>
        public IQSearchClient<TSearchResultSet, TSearchResult> CreateSearchClient<TSearchResultSet, TSearchResult>(Uri endpoint, string searchIndex)
            where TSearchResultSet : IQueryResultData<TSearchResult> where TSearchResult : IQueryResult => new IQSearchClient<TSearchResultSet, TSearchResult>(endpoint, _oauth, searchIndex);

        /// <summary>
        /// Return a fully constructed Public Content Api client
        /// </summary>
        /// <returns>Public Content Api Client</returns>
        public PublicContentApi.PublicContentApi CreateClient()
        {
            var graphQL = new GraphQLClient.GraphQLClient(_endpoint, new Logger(), _oauth);
            var client = new PublicContentApi.PublicContentApi(graphQL, new Logger());
            // just make sure our requests come back as R2 json
            client.DefaultModelType = DataModelType.R2;
            // add context data to client
            IClaimStore claimStore = AmbientDataContext.CurrentClaimStore;
            if (claimStore == null)
            {
                Log.Warn("No claimstore found so unable to populate claims for PCA.");
            }
            
            Dictionary<string, string[]> headers =
                claimStore?.Get<Dictionary<string, string[]>>(new Uri(WebClaims.REQUEST_HEADERS));
            if (headers != null && headers.ContainsKey(PreviewSessionTokenHeader))
            {
                client.HttpClient.Headers[PreviewSessionTokenHeader] = headers[PreviewSessionTokenHeader];
            }

            Dictionary<string, string> cookies =
                 claimStore?.Get<Dictionary<string, string>>(new Uri(WebClaims.REQUEST_COOKIES));
            if (cookies != null && cookies.ContainsKey(PreviewSessionTokenCookie))
            {
                client.HttpClient.Headers[PreviewSessionTokenHeader] = cookies[PreviewSessionTokenCookie];              
            }
           
            if (!_claimForwarding) return client;
            // Forward all claims
            var forwardedClaimValues = AmbientDataContext.ForwardedClaims;
            if (forwardedClaimValues == null || forwardedClaimValues.Count <= 0) return client;
            Dictionary<Uri, object> forwardedClaims =
                forwardedClaimValues.Select(claim => new Uri(claim, UriKind.RelativeOrAbsolute))
                    .Distinct()
                    .Where(uri => claimStore.Contains(uri) && claimStore.Get<object>(uri) != null)
                    .ToDictionary(uri => uri, uri => claimStore.Get<object>(uri));

            if (forwardedClaims.Count <= 0) return client;

            foreach (var claim in forwardedClaims)
            {
                client.GlobalContextData.ClaimValues.Add(new ClaimValue
                {
                    Uri = claim.Key.ToString(),
                    Value = claim.Value.ToString(),
                    Type = ClaimValueType.STRING
                });
            }

            return client;
        }
    }
}
