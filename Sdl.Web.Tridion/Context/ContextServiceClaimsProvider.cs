using System;
using System.Collections.Generic;
using System.Web;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Context.Api;
using Sdl.Web.Context.Api.Resolution;
using Sdl.Web.Context.OData.Client;

namespace Sdl.Web.Tridion.Context
{
    /// <summary>
    /// DXA Context Claims Provider using the SDL Web 8 CDaaS Context Service.
    /// </summary>
    /// <remarks>
    /// This class is excluded in the Release_71 configuration.
    /// </remarks>
    public class ContextServiceClaimsProvider : IContextClaimsProvider
    {
        #region IContextClaimsProvider Members

        /// <summary>
        /// Gets the context claims. Either all context claims or for a given aspect name.
        /// </summary>
        /// <param name="aspectName">The aspect name. If <c>null</c> all context claims are returned.</param>
        /// <returns>A dictionary with the claim names in format aspectName.propertyName as keys.</returns>
        public IDictionary<string, object> GetContextClaims(string aspectName)
        {
            using (new Tracer(aspectName))
            {
                // TODO: Not really nice to use HttpContext at this level.
                HttpContext httpContext = HttpContext.Current;
                if (httpContext == null)
                {
                    throw new DxaException("Cannot obtain HttpContext.");
                }
                HttpRequest httpRequest = httpContext.Request;
                HttpCookie contextCookie = httpRequest.Cookies["context"];

                IContextMap contextMap;
                try
                {
                    EvidenceBuilder evidenceBuilder = new EvidenceBuilder().With("user-agent", httpRequest.UserAgent);
                    if (contextCookie != null && !string.IsNullOrEmpty(contextCookie.Value))
                    {
                        evidenceBuilder.With("cookie", contextCookie.Value);
                    }
                    IEvidence evidence = evidenceBuilder.Build();

                    ODataContextEngine contextEngineClient = new ODataContextEngine();
                    contextMap = contextEngineClient.Resolve(evidence);
                }
                catch (Exception ex)
                {
                    throw new DxaException("An error occurred while resolving evidence using the Context Service.", ex);
                }

                IDictionary<string, object> result = new Dictionary<string, object>();
                if (string.IsNullOrEmpty(aspectName))
                {
                    // Add claims for all aspects.
                    foreach (string aspectKey in contextMap.KeySet)
                    {
                        AddAspectClaims(aspectKey, contextMap, result);
                    }
                }
                else
                {
                    // Add claims for the given aspect.
                    AddAspectClaims(aspectName, contextMap, result);
                }

                return result;
            }
        }

        /// <summary>
        /// Gets the device family (an aggregated device claim determined from other context claims).
        /// </summary>
        /// <returns>A string representing the device family.</returns>
        public string GetDeviceFamily()
        {
            // TODO TSI-789: this functionality overlaps with "Context Expressions".
            using (new Tracer())
            {
                return null; // Returning null here triggers default implementation in ContextEngine.
            }
        }
        #endregion

        private void AddAspectClaims(string aspectName, IContextMap contextMap, IDictionary<string, object> claims)
        {
            IAspect aspect = contextMap.Get(aspectName);
            foreach (string propertyName in aspect.KeySet)
            {
                string claimName = string.Format("{0}.{1}", aspectName, propertyName);
                object claimValue = aspect.Get(propertyName);
                claims.Add(claimName, claimValue);
            }
        }
    }
}
