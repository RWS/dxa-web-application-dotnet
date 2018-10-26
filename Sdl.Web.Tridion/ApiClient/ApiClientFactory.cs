﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Configuration;
using Sdl.Tridion.Api.Client;
using Sdl.Tridion.Api.Client.ContentModel;
using Sdl.Tridion.Api.GraphQL.Client;
using Sdl.Tridion.Api.Http.Client.Auth;
using Sdl.Tridion.Api.IQQuery.API;
using Sdl.Tridion.Api.IQQuery.Client;
using Sdl.Web.Common.Logging;
using Sdl.Web.Delivery.DiscoveryService;
using Sdl.Web.Delivery.ServicesCore.ClaimStore;

namespace Sdl.Web.Tridion.ApiClient
{
    /// <summary>
    /// Api Client Factory creates clients with context claim forwarding and
    /// OAuthentication for using the GraphQL Api.
    /// </summary>
    public sealed class ApiClientFactory
    {
        private readonly Uri _endpoint;
        private readonly Uri _iqEndpoint;
        private readonly string _iqSearchIndex;
        private readonly bool _claimForwarding = true;
        private readonly IAuthentication _oauth;
        private const string PreviewSessionTokenHeader = "x-preview-session-token";
        private const string PreviewSessionTokenCookie = "preview-session-token";

        private static readonly Lazy<ApiClientFactory> lazy =
            new Lazy<ApiClientFactory>(() => new ApiClientFactory());

        public static ApiClientFactory Instance => lazy.Value;
        
        private ApiClientFactory()
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
                    throw new ApiClientException("Unable to retrieve endpoint for Public Content Api");
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
        public Sdl.Tridion.Api.Client.ApiClient CreateClient()
        {
            var graphQL = new GraphQLClient(_endpoint, new Logger(), _oauth);
            var client = new Sdl.Tridion.Api.Client.ApiClient(graphQL, new Logger());
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
                    .Where(uri => claimStore.Contains(uri) && claimStore.Get<object>(uri) != null && !uri.ToString().Equals("taf:session:preview:preview_session"))
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