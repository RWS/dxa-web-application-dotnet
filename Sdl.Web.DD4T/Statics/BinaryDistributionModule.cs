using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Logging;
using Sdl.Web.Mvc.Configuration;
using System.Collections.Generic;

namespace Sdl.Web.DD4T.Statics
{
    /// <summary>
    /// HttpModule intercepting a request to a static resource, caches the resource to the file-system from the Broker DB
    /// </summary>
    public class BinaryDistributionModule : IHttpModule  // TODO TSI-788: This class doesn't belong in Sdl.Web.DD4T
    {
        private static readonly Dictionary<string, Regex> _localizationBinaryRegexes = new Dictionary<string, Regex>();
        private static IBinaryFileManager _binaryFileManager;


        #region IHttpModule members
        /// <summary>
        /// Initialize this HttpModule.
        /// </summary>
        /// <param name="application">Current HttpApplication</param>
        public void Init(HttpApplication application) 
        {
            application.PreRequestHandlerExecute += OnPreRequestHandlerExecute;
            application.BeginRequest += OnBeginRequest;
        }

        /// <summary>
        /// Dispose the HttpModule.
        /// </summary>
        public void Dispose()
        {
            // Nothing to do.
        }
        #endregion


        /// <summary>
        /// Main method handling requests to the specified resource.
        /// </summary>
        /// <param name="o">Current HttpApplication</param>
        /// <param name="eventArgs">Current event arguments</param>
        private static void OnPreRequestHandlerExecute(object o, EventArgs eventArgs)
        {
            HttpApplication application = (HttpApplication)o;
            HttpContext context = application.Context;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;
            string urlPath = request.Url.AbsolutePath;

            using (new Tracer(o, eventArgs, urlPath))
            {
                if (WebRequestContext.HasNoLocalization)
                {
                    return;
                }

                string staticsRootUrl = SiteConfiguration.GetLocalStaticsUrl(WebRequestContext.Localization.LocalizationId);
                urlPath = urlPath.StartsWith("/" + staticsRootUrl) ? urlPath.Substring(staticsRootUrl.Length + 1) : urlPath;
                Regex binaryUrlRegex = GetBinaryUrlRegex(WebRequestContext.Localization);
                if (!binaryUrlRegex.IsMatch(urlPath))
                {
                    Log.Debug("URL '{0}' does not match binary URL pattern '{1}', ignoring it.", urlPath, binaryUrlRegex);
                    return;
                }

                if (!BinaryFileManager.ProcessRequest(request))
                {
                    Log.Debug("No Binary found for URL '{0}'. Returning HTTP 404 (Not Found).", urlPath);
                    response.StatusCode = 404;
                    response.SuppressContent = true;
                    application.CompleteRequest();
                    return;
                }
                // if we got here, the file was successfully created on file-system
                DateTime ifModifiedSince = Convert.ToDateTime(request.Headers["If-Modified-Since"]);
                Log.Debug("If-Modified-Since: " + ifModifiedSince);

                DateTime fileLastModified = File.GetLastWriteTime(request.PhysicalPath);
                Log.Debug("File last modified: " + fileLastModified);

                if (fileLastModified.Subtract(ifModifiedSince).TotalSeconds < 1)
                {
                    Log.Debug("Sending HTTP 304 (Not Modified).");
                    response.StatusCode = 304;
                    response.SuppressContent = true;
                    application.CompleteRequest();
                    return;
                }

                // Note: if the file was just created, an empty dummy might still be served by IIS
                // To make sure the right file is sent, we will transmit the file directly within the first second of the creation
                if (fileLastModified.AddSeconds(1).CompareTo(DateTime.Now) > 0)
                {
                    Log.Debug("File was created less than 1 second ago, transmitting content directly.");
                    response.Clear();
                    try
                    {
                        response.TransmitFile(request.PhysicalPath);
                    }
                    catch (IOException ex)
                    {
                        // file probabaly accessed by a different thread in a different process
                        Log.Error("TransmitFile failed: {0}\r\n{1}", ex.Message, ex.StackTrace);
                    }
                }
            }
        }

        private static void OnBeginRequest(Object source, EventArgs e)
        {
            HttpContext context = HttpContext.Current;
            HttpRequest request = context.Request;
            string urlPath = request.Url.AbsolutePath;

            using (new Tracer(source, e, urlPath))
            {
                if (WebRequestContext.HasNoLocalization)
                {
                    return;
                }

                Regex binaryUrlRegex = GetBinaryUrlRegex(WebRequestContext.Localization);
                if (!binaryUrlRegex.IsMatch(urlPath))
                {
                    Log.Debug("URL '{0}' does not match binary URL pattern '{1}', ignoring it.", urlPath, binaryUrlRegex);
                    return;
                }

                string realPath = Path.Combine(new[] { request.PhysicalApplicationPath, SiteConfiguration.GetLocalStaticsFolder(WebRequestContext.Localization.LocalizationId), request.Path.ToCombinePath() });
                context.RewritePath("/" + SiteConfiguration.GetLocalStaticsFolder(WebRequestContext.Localization.LocalizationId) + request.Path);
                if (!File.Exists(realPath))
                {
                    string dir = realPath.Substring(0, realPath.LastIndexOf("\\", StringComparison.Ordinal));
                    Log.Debug("Dir path: {0}", dir);
                    try
                    {
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        lock (NamedLocker.GetLock(realPath))
                        {
                            File.Create(realPath).Dispose();
                        }
                    }
                    catch (IOException)
                    {
                        // file probabaly accessed by a different thread in a different process, locking failed
                        Log.Warn("Cannot create {0}. This can happen sporadically, let the next thread handle this.", realPath);
                    }
                }
            }
        }

        #region private members
        private static IBinaryFileManager BinaryFileManager  // TODO TSI-788: Use Dependency injection (or merge BinaryFileManager into BinaryDistributionModule)
        {
            get
            {
                return _binaryFileManager ?? (_binaryFileManager = new BinaryFileManager());
            }
        }

        private static Regex GetBinaryUrlRegex(Localization localization)
        {
            string localizationId = localization.LocalizationId;
            lock (_localizationBinaryRegexes)
            {
                Regex result;
                if (_localizationBinaryRegexes.TryGetValue(localizationId, out result))
                {
                    return result;
                }
                result = new Regex(localization.MediaUrlRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                _localizationBinaryRegexes.Add(localizationId, result);
                return result;
            }
        }

        #endregion
    }
}