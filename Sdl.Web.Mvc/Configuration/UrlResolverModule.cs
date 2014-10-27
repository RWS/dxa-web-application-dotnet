using System;
using System.Web;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Mvc.Configuration;

namespace Sdl.Web.Mvc.Configuration
{
    /// <summary>
    /// Module to establish the Localization (Publication) which a URL request corresponds to
    /// </summary>
    public class UrlResolverModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.BeginRequest += BeginRequest;
        }

        private static void BeginRequest(object sender, EventArgs e)
        {
            var loc = WebRequestContext.Localization;
            //Attempt to get current localization, if successful this will be cached for the whole request
            if (loc == null)
            {
                //if unsuccesful, log an information message, but carry on - there may be assets managed outside of the CMS managed URLs
                Log.Info("Request URL {0} does not map to a localization managed by this web application.", HttpContext.Current.Request.Url);
            }
            else
            {
                Log.Debug("Request URL {0} maps to localization {1}", HttpContext.Current.Request.Url, loc.LocalizationId);
            }
        }

        public void Dispose()
        {
            //do nothing
        }
    }
}
