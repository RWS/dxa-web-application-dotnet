using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using DD4T.Utils;
using DD4T.ContentModel.Contracts.Logging;
using System.Web.Mvc;
using DD4T.ContentModel.Contracts.Configuration;

namespace DD4T.Web.Binaries
{

    /// <summary>
    /// HttpModule intercepting a request to a static resource, caches the resource to the file-system from the Broker DB
    /// </summary>
    public class BinaryDistributionModule : IHttpModule
    {

        private static ILogger _logger;
        private static IDD4TConfiguration _configuration;
        #region IHttpModule
        /// <summary>
        /// Initialize this module. Attach the worker method to the BeginRequest event.
        /// </summary>
        /// <param name="application">Current HttpApplication</param>
        public void Init(HttpApplication application)
        {
            _logger = DependencyResolver.Current.GetService<ILogger>();
            _configuration = DependencyResolver.Current.GetService<IDD4TConfiguration>();
            application.PreRequestHandlerExecute += new EventHandler(DistributionModule_OnPreRequestHandlerExecute);
            application.BeginRequest += new EventHandler(DistributionModule_OnBeginRequest);
        }

        /// <summary>
        /// Main method handling requests to the specified resource.
        /// </summary>
        /// <param name="o">Current HttpApplication</param>
        /// <param name="eventArgs">Current event arguments</param>
        public void DistributionModule_OnPreRequestHandlerExecute(object o, EventArgs eventArgs)
        {
            DateTime start = DateTime.Now;
            HttpApplication application = (HttpApplication)o;
            HttpContext context = application.Context;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;
            string urlPath = request.Url.AbsolutePath;
            if (!IsBinaryUrl.IsMatch(urlPath))
            {
                _logger.Debug("url {0} does not match binary url pattern, ignoring it", urlPath);
                return;
            }

            if (!BinaryFileManager.ProcessRequest(request))
            {
                _logger.Debug("Url {0} not found. Returning 404 Not Found.", urlPath);
                response.StatusCode = 404;
                response.SuppressContent = true;
                application.CompleteRequest();
                return;
            }
            // if we got here, the file was successfully created on file-system
            DateTime ifModifiedSince = Convert.ToDateTime(request.Headers["If-Modified-Since"]);
            _logger.Debug("If-Modified-Since: " + ifModifiedSince);

            DateTime fileLastModified = File.GetLastWriteTime(request.PhysicalPath);
            _logger.Debug("File last modified: " + fileLastModified);

            if (fileLastModified.Subtract(ifModifiedSince).TotalSeconds < 1)
            {
                _logger.Debug("Sending 304 Not Modified");
                response.StatusCode = 304;
                response.SuppressContent = true;
                application.CompleteRequest();
            }

            // Note: if the file was just created, an empty dummy might still be served by IIS
            // To make sure the right file is sent, we will transmit the file directly within the first second of the creation
            if (fileLastModified.AddSeconds(1).CompareTo(DateTime.Now) > 0)
            {
                _logger.Debug("file was created less than 1 second ago, transmitting content directly");
                response.Clear();
                response.TransmitFile(request.PhysicalPath);
            }
        }


        public static void DistributionModule_OnBeginRequest(Object source, EventArgs e)
        {

            HttpContext context = HttpContext.Current;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            string urlPath = request.Url.AbsolutePath;
            _logger.Information(">>DistributionModule_OnBeginRequest ({0})", urlPath);

            if (!IsBinaryUrl.IsMatch(urlPath))
            {
                _logger.Debug("url {0} does not match binary url pattern, ignoring it", urlPath);
                _logger.Information("<<DistributionModule_OnBeginRequest ({0})", urlPath);
                return;
            }

            string realPath = request.PhysicalApplicationPath + "BinaryData" + request.Path.Replace("/", "\\"); // request.PhysicalPath;
            context.RewritePath("/BinaryData" + request.Path);

            if (!File.Exists(realPath))
            {
                _logger.Debug("Dir path: " + realPath.Substring(0, realPath.LastIndexOf("\\")));
                try
                {
                    string dir = realPath.Substring(0, realPath.LastIndexOf("\\"));

                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    using (FileStream file = File.Create(realPath))
                    {
                        StreamWriter sw = new StreamWriter(file);
                        sw.Write("");
                        sw.Close();
                        file.Close();
                    }
                }
                catch (Exception exception)
                {
                    _logger.Information("IIS empty file could not be created." + exception.Message);
                }
            }

            _logger.Information("<<DistributionModule_OnBeginRequest ({0})", urlPath);
        }

        /// <summary>
        /// Do nothing
        /// </summary>
        public void Dispose() { }


        #endregion

        #region private
        private IBinaryFileManager _binaryFileManager = null;
        public virtual IBinaryFileManager BinaryFileManager
        {
            get
            {
                if (_binaryFileManager == null)
                    _binaryFileManager = new BinaryFileManager();
                return _binaryFileManager;
            }
            set
            {
                _binaryFileManager = value;
            }
        }
        private static Regex _isBinaryUrl = null;
        private static Regex IsBinaryUrl
        {
            get
            {
                if (_isBinaryUrl == null)
                    _isBinaryUrl = new Regex(_configuration.BinaryUrlPattern);
                return _isBinaryUrl;
            }
        }

        #endregion
    }
}
