using System;
using System.Net;
using Sdl.Web.Delivery.DiscoveryService;
using Sdl.Web.Delivery.Service;
using Sdl.Web.HttpClient.Auth;
using Sdl.Web.HttpClient.Request;

namespace Sdl.Web.Tridion.PCAClient
{
    public class OAuth : IAuthentication
    {
        private readonly IOAuthTokenProvider _tokenProvider;

        public OAuth(IOAuthTokenProvider tokenProvider)
        {
            _tokenProvider = tokenProvider;
        }

        public NetworkCredential GetCredential(Uri uri, string authType) => null;

        /// <summary>
        /// Add OAuth headers to http request.
        /// <remarks>
        /// The CIL TokenProvider implementation handles aquiring/refreshing tokens from the
        /// token service so we can be sure that on call to this our OAuth token is valid.
        /// </remarks>
        /// </summary>
        /// <param name="request">Http Request</param>
        public void ApplyManualAuthentication(IHttpClientRequest request)
        {
            // no token provider means no need to add OAuth token
            if (_tokenProvider == null) return;
            request.Headers.Add(
                DiscoveryServiceProvider.DefaultTokenProvider.AuthRequestHeaderName,
                DiscoveryServiceProvider.DefaultTokenProvider.AuthRequestHeaderValue);
        }
    }
}
