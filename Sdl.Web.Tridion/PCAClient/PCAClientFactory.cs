using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Delivery.DiscoveryService;
using Sdl.Web.Delivery.ServicesCore.ClaimStore;
using Sdl.Web.HttpClient.Auth;
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
        private readonly IAuthentication _oauth;
        private const string PreviewSessionTokenHeader = "x-preview-session-token";
        private const string PreviewSessionTokenCookie = "preview-session-token";

        private static readonly Lazy<PCAClientFactory> lazy =
            new Lazy<PCAClientFactory>(() => new PCAClientFactory());

        public static PCAClientFactory Instance => lazy.Value;
        
        private PCAClientFactory()
        {
            string uri = WebConfigurationManager.AppSettings["pca-service-uri"];
            if (string.IsNullOrEmpty(uri))
            {
                IDiscoveryService discoveryService = DiscoveryServiceProvider.Instance.ServiceClient;
                _endpoint = new Uri(discoveryService.ContentServiceUri.AbsoluteUri.Replace("content.svc",
                    "udp/content"));
            }
            else
            {
                _endpoint = new Uri(uri);
            }
            if (_endpoint == null)
            {
                throw new PCAClientException("Unable to retrieve endpoint for Public Content Api");
            }

            _oauth = new OAuth(DiscoveryServiceProvider.DefaultTokenProvider);
            Log.Debug($"PCAClient found at URL '{_endpoint}'.");
        }

        /// <summary>
        /// Return a fully constructed Public Content Api client
        /// </summary>
        /// <returns>Public Content Api Client</returns>
        public PublicContentApi.PublicContentApi CreateClient()
        {
            var graphQL = new GraphQLClient.GraphQLClient(_endpoint, new Logger(), _oauth);
            var client = new PublicContentApi.PublicContentApi(graphQL, new Logger());

            // add context data to client
            IClaimStore claimStore = AmbientDataContext.CurrentClaimStore;
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
                // todo:
                //client.HttpClient.Cookies.Add(cookies[PreviewSessionTokenCookie]);
            }

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
