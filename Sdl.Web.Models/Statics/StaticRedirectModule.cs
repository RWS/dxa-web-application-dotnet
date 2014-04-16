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
        private static string VERSION_REGEX = @"system/(v\d*.\d*/)";
        public void Init(HttpApplication context)
        {
            context.BeginRequest += new EventHandler(this.BeginRequest);
        }

        private void BeginRequest(object sender, EventArgs e)
        {
            HttpApplication application = (HttpApplication)sender;
            HttpContext context = application.Context;
            bool rewrite = false;
            var url = Regex.Replace(context.Request.Url.AbsolutePath, VERSION_REGEX, delegate(Match match)
            {
                rewrite = true;
                return "system/";
            });
            if (rewrite)
            {
                //does it make sense to do a 301 redirect? http://msdn.microsoft.com/en-us/library/system.web.httpresponse.redirectlocation(v=vs.110).aspx
                context.Response.Redirect(url);
            }
        }

        public void Dispose()
        {
            //do nothing
        }
    }
}
