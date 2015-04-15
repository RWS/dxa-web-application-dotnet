using System;
using System.Web;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Mvc.Configuration;

namespace Sdl.Web.Mvc.Statics
{
    /// <summary>
    /// Module to redirect requests for version of static files which no longer exist on disk to the root assets folder
    /// When a request for a static file comes into IIS, it first checks if it exists on disk, and if so, serves it normally
    /// If it does not exist, it enters the ASP.NET pipeline, where this module will pick it up and redirect it
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
            var url = context.Request.Url.AbsolutePath;
            Log.Debug("StaticRedirectModule_BeginRequest: " + url);
            var versionLessUrl = SiteConfiguration.RemoveVersionFromPath(url);
            if (url != versionLessUrl)
            {
                Log.Debug("Rewriting request for non-existent versioned static file {0} to {1}", url, versionLessUrl);
                context.RewritePath(versionLessUrl);
            }
        }

        public void Dispose()
        {
            //do nothing
        }
    }
}
