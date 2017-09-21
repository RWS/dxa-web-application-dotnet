using System;
using System.Net;
using System.Web;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;

namespace Sdl.Web.Mvc.Statics
{
    /// <summary>
    /// HttpModule handling requests for static content items, including versioned URL rewriting.
    /// </summary>
    public class StaticContentModule : IHttpModule
    {
        private const string IsVersionedUrlContextItem = "IsVersionedUrl";

        #region IHttpModule members
        /// <summary>
        /// Initialize this HttpModule.
        /// </summary>
        /// <param name="application">Current HttpApplication</param>
        public void Init(HttpApplication application) 
        {
            application.BeginRequest += BeginRequest;
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

        private static void BeginRequest(object sender, EventArgs e)
        {
            HttpApplication application = (HttpApplication)sender;
            HttpContext context = application.Context;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;
            string url = request.Url.AbsolutePath;

            using (new Tracer(sender, e, url))
            {
                // Attempt to determine Localization
                Localization localization = null;
                try
                {
                    localization = WebRequestContext.Localization;
                }
                catch (DxaUnknownLocalizationException ex)
                {
                    IUnknownLocalizationHandler unknownLocalizationHandler = SiteConfiguration.UnknownLocalizationHandler;
                    if (unknownLocalizationHandler != null)
                    {
                        localization = unknownLocalizationHandler.HandleUnknownLocalization(ex, request, response);
                    }

                    if (localization == null)
                    {
                        // There was no Unknown Localization Handler or it didn't terminate the request processing using response.End()
                        // and it also didn't resolve a Localization.
                        SendNotFoundResponse(ex.Message, response);
                    }
                }
                catch (DxaItemNotFoundException  ex)
                {
                    // Localization has been resolved, but could not be initialized (e.g. Version.json not found)
                    Log.Error(ex);
                    SendNotFoundResponse(ex.Message, response);
                }
                catch (Exception ex)
                {
                    // Other exceptions: log and let ASP.NET handle them
                    Log.Error(ex);
                    throw;
                }
                Log.Debug("Request URL '{0}' maps to Localization [{1}]", request.Url, localization);

                // Prevent direct access to BinaryData folder
                if (url.StartsWith("/" + SiteConfiguration.StaticsFolder + "/"))
                {
                    SendNotFoundResponse(string.Format("Attempt to directly access the static content cache through URL '{0}'", url), response);
                }

                // Rewrite versioned URLs
                string versionLessUrl = SiteConfiguration.RemoveVersionFromPath(url);
                if (url != versionLessUrl)
                {
                    Log.Debug("Rewriting versioned static content URL '{0}' to '{1}'", url, versionLessUrl);
                    context.RewritePath(versionLessUrl);
                    context.Items[IsVersionedUrlContextItem] = true;
                }
            }
        }

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
                Localization localization = WebRequestContext.Localization;
                string staticsRootUrl = localization.BinaryCacheFolder.Replace("\\", "/");
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
                        if (lastModified <= ifModifiedSince.AddSeconds(1))
                        {
                            Log.Debug("Static content item last modified at {0} => Sending HTTP 304 (Not Modified).", lastModified);
                            response.StatusCode = (int) HttpStatusCode.NotModified;
                            response.SuppressContent = true;
                        }
                        else
                        {
                            if (!localization.IsXpmEnabled)
                            {
                                // Items with a versioned URL can be cached long-term, because the URL will change if needed.
                                bool isVersionedUrl = context.Items.Contains(IsVersionedUrlContextItem);

                                TimeSpan maxAge = isVersionedUrl ? new TimeSpan(7, 0, 0, 0) : new TimeSpan(0, 1, 0, 0); // 1 Week or 1 Hour

                                response.Cache.SetCacheability(HttpCacheability.Private); // Allow caching
                                response.Cache.SetMaxAge(maxAge);
                                response.Cache.SetExpires(DateTime.UtcNow.Add(maxAge));
                            }

                            response.Cache.SetLastModified(lastModified); // Allows the browser to do an If-Modified-Since request next time
                            response.ContentType = staticContentItem.ContentType;
                            staticContentItem.GetContentStream().CopyTo(response.OutputStream);
                        }
                    }
                }
                catch (DxaItemNotFoundException ex)
                {
                    SendNotFoundResponse(ex.Message, response);
                }
                catch (Exception ex)
                {
                    // Other exceptions: log and let ASP.NET handle them
                    Log.Error(ex);
                    throw;
                }

                // Terminate the HTTP pipeline.
                application.CompleteRequest();
            }

        }

        private static void SendNotFoundResponse(string message, HttpResponse httpResponse)
        {
            Log.Warn("{0}. Sending HTTP 404 (Not Found) response.", message);
            httpResponse.StatusCode = (int)HttpStatusCode.NotFound;
            httpResponse.ContentType = "text/plain";
            httpResponse.Write(message);
            httpResponse.End(); // This terminates the HTTP processing pipeline
        }
    }
}