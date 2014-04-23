using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Sdl.Web.Mvc
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
            context.BeginRequest += new EventHandler(this.BeginRequest);
        }

        private void BeginRequest(object sender, EventArgs e)
        {
            HttpApplication application = (HttpApplication)sender;
            HttpContext context = application.Context;
            var url = context.Request.Url.AbsolutePath;
            var versionLessUrl = Configuration.RemoveVersionFromPath(url);
            if (url != versionLessUrl)
            {
                context.Response.Redirect(versionLessUrl);
            }
        }

        public void Dispose()
        {
            //do nothing
        }
    }
}
