using System;
using System.Web;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Mvc.Configuration;

namespace Sdl.Web.Mvc.Statics
{
    /// <summary>
    /// This module has 3 tasks:
    /// 1. Try to get the Localization matching the request URL (if successful this is cached for the rest of the request)
    /// 2. Prevent direct requests for binary content that has been serialized to disk - we serialize
    ///    binaries to the /BinaryData folder of the website, and binary requests are rewritten to this
    ///    path, however in the situation where mutiple sites are hosted by the same web application we 
    ///    dont want to allow binaries from one site to be shown on the other. Normally this would be possible
    ///    if you know the publication id and the URL of the image using the a URL like:
    ///    /BinaryData/12/media/image-from-another-site.jpg
    /// 3. Remove the version from the URL of HTML design assets - links to design assets are written out 
    ///    with a version number in the URL (eg /system/v0.60/assets/css/main.css). This version
    ///    does not correspond to any real path, but is purely a mechanism to allow us to avoid
    ///    browser caching issues when the css is updated (we simply update the version number)
    /// </summary>
    public class StaticRedirectModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.BeginRequest += BeginRequest;
        }

        private static void BeginRequest(object sender, EventArgs e)
        {
            HttpApplication application = (HttpApplication)sender;
            HttpContext context = application.Context;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;
            string url = context.Request.Url.AbsolutePath;

            using (new Tracer(sender,e, url))
            {
                //1. Attempt to get current localization
                Localization loc = WebRequestContext.Localization;
                if (loc == null)
                {
                    WebRequestContext.HasNoLocalization = true;
                    //if unsuccesful, log an information message, but carry on - there may be assets managed outside of the CMS managed URLs
                    Log.Warn("Request URL '{0}' does not map to a Localization managed by this web application.", HttpContext.Current.Request.Url);
                }
                else
                {
                    Log.Debug("Request URL '{0}' maps to Localization [{1}]", request.Url, loc);
                }

                //2. Block direct access to BinaryData folder
                if (url.StartsWith("/" + SiteConfiguration.StaticsFolder + "/"))
                {
                    Log.Warn("Attempt to directly access the static content cache through URL '{0}'. Sending HTTP 404 (Not Found) response.", url);
                    response.StatusCode = 404;
                    response.End();
                    return;
                }

                //3. Remove version from URL
                string versionLessUrl = SiteConfiguration.RemoveVersionFromPath(url);
                if (url != versionLessUrl)
                {
                    Log.Debug("Rewriting versioned static content URL '{0}' to '{1}'", url, versionLessUrl);
                    context.RewritePath(versionLessUrl);
                }
            }
        }

        public void Dispose()
        {
            //do nothing
        }
    }
}
