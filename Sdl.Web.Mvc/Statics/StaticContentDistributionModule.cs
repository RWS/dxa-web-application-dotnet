using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using System.Collections.Generic;

namespace Sdl.Web.Mvc.Statics
{
    /// <summary>
    /// HttpModule intercepting requests for static content items, retrieving those from the Content Provider and returning the (binary) content.
    /// </summary>
    public class StaticContentDistributionModule : IHttpModule
    {
        #region IHttpModule members
        /// <summary>
        /// Initialize this HttpModule.
        /// </summary>
        /// <param name="application">Current HttpApplication</param>
        public void Init(HttpApplication application) 
        {
            application.PreRequestHandlerExecute += OnPreRequestHandlerExecute;
        }

        /// <summary>
        /// Disposes the HttpModule.
        /// </summary>
        public void Dispose()
        {
            // Nothing to do.
        }
        #endregion


        /// <summary>
        /// Event handler that gets triggered just before the ASP.NET Request Handler gets executed.
        /// </summary>
        /// <param name="sender">The <see cref="HttpApplication"/> sending the event.</param>
        /// <param name="eventArgs">The event arguments.</param>
        private static void OnPreRequestHandlerExecute(object sender, EventArgs eventArgs)
        {
            HttpApplication application = (HttpApplication)sender;
            HttpContext context = application.Context;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;
            string urlPath = request.Url.AbsolutePath;
            DateTime ifModifiedSince = Convert.ToDateTime(request.Headers["If-Modified-Since"]);

            using (new Tracer(sender, eventArgs, urlPath, ifModifiedSince))
            {
                if (WebRequestContext.HasNoLocalization)
                {
                    return;
                }

                Localization localization = WebRequestContext.Localization;
                string staticsRootUrl = SiteConfiguration.GetLocalStaticsUrl(localization.LocalizationId);
                urlPath = urlPath.StartsWith("/" + staticsRootUrl) ? urlPath.Substring(staticsRootUrl.Length + 1) : urlPath;
                if (!localization.IsStaticContentUrl(urlPath))
                {
                    // Not a static content item; continue the HTTP pipeline.
                    return;
                }

                try
                {
                    using (StaticContentItem staticContentItem = SiteConfiguration.ContentProvider.GetStaticContentItem(urlPath, localization))
                    {
                        DateTime lastModified = staticContentItem.LastModified;
                        if (lastModified < ifModifiedSince)
                        {
                            Log.Debug("Static content item last modified at {0} => Sending HTTP 304 (Not Modified).", lastModified);
                            response.StatusCode = (int) HttpStatusCode.NotModified;
                            response.SuppressContent = true;
                        }
                        else
                        {
                            response.ContentType = staticContentItem.ContentType;
                            staticContentItem.GetContentStream().CopyTo(response.OutputStream);
                        }
                    }
                }
                catch (DxaItemNotFoundException e)
                {
                    Log.Warn("{0}. Returning HTTP 404 (Not Found).", e.Message);
                    response.StatusCode = (int) HttpStatusCode.NotFound;
                    response.SuppressContent = true;
                }

                // Terminate the HTTP pipeline.
                application.CompleteRequest(); // TODO: use response.End() instead?
            }
        }
    }
}