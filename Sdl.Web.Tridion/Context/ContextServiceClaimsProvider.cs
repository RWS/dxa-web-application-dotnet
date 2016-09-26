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
        private static readonly ODataContextEngine _contextEngineClient;

        /// <summary>
        /// Class constructor
        /// </summary>
        static ContextServiceClaimsProvider()
        {
            using (new Tracer())
            {
                try
                {
                    _contextEngineClient = new ODataContextEngine();
                }
                catch (Exception ex)
                {
                    // ODataContextEngine construction can fail for several reasons, because it immediately tries to communicate with Discovery Service.
                    // Error handling in ODataContextEngine is currently suboptimal and class ContextServiceClaimsProvider is constructed very early in the DXA initialization.
                    // Therefore, we just log the error here and continue; GetContextClaims will throw an exception later on (if we even get to that point).
                    Log.Error(ex);
                }
            }
        }

        /// <summary>
        /// Default User Agent used in case no User-Agent HTTP header is found (mainly used for testing purposes).
        /// </summary>
        public string DefaultUserAgent { get; set; }

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
                if (_contextEngineClient == null)
                {
                    // Apparently an exception occurred in the class constructor; it should have logged the exception already.
                    throw new DxaException("Context Engine Client was not initialized. Check the log file for errors.");
                }

                string userAgent = null;
                string contextCookieValue = null;
                HttpContext httpContext = HttpContext.Current; // TODO: Not really nice to use HttpContext at this level.
                if (httpContext != null)
                {
                    userAgent = httpContext.Request.UserAgent;
                    HttpCookie contextCookie = httpContext.Request.Cookies["context"];
                    if (contextCookie != null)
                    {
                        contextCookieValue = contextCookie.Value;
                    }
                }
                if (string.IsNullOrEmpty(userAgent))
                {
                    userAgent = DefaultUserAgent;
                }

                IContextMap contextMap;
                try
                {
                    EvidenceBuilder evidenceBuilder = new EvidenceBuilder().With("user-agent", userAgent);
                    if (!string.IsNullOrEmpty(contextCookieValue))
                    {
                        evidenceBuilder.With("cookie", string.Format("context={0}", contextCookieValue));
                    }
                    IEvidence evidence = evidenceBuilder.Build();
                    contextMap = _contextEngineClient.Resolve(evidence);
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
