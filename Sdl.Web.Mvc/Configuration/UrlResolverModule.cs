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
            //Attempt to get current localization, if successful this will be cached for the whole request
            if (WebRequestContext.Localization == null)
            {
                //if unsuccesful, throw an error and do not process the request any further
                var ex = new Exception("Request URL does not map to a localization managed by this web application.");
                Log.Error(ex);
                throw ex;
            }
        }

        public void Dispose()
        {
            //do nothing
        }
    }
}
