using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;

namespace Sdl.Web.DD4T.Statics
{
	/// <summary>
	/// HttpModule intercepting a request to a static resource, caches the resource to the file-system from the Broker DB
	/// </summary>
	public class BinaryDistributionModule : IHttpModule
    {
        #region IHttpModule
        /// <summary>
		/// Initialize this module. Attach the worker method to the BeginRequest event.
		/// </summary>
		/// <param name="application">Current HttpApplication</param>
		public void Init(HttpApplication application) 
        {
            application.PreRequestHandlerExecute += DistributionModule_OnPreRequestHandlerExecute;
			application.BeginRequest += DistributionModule_OnBeginRequest;
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
            urlPath = urlPath.StartsWith("/" + SiteConfiguration.StaticsFolder) ? urlPath.Substring(SiteConfiguration.StaticsFolder.Length + 1) : urlPath;
            if (!IsBinaryUrl.IsMatch(urlPath))
            {
                Log.Debug("Url {0} does not match binary url pattern, ignoring it.", urlPath);
                Log.Trace(start, "binary-ignored", response.StatusCode.ToString(CultureInfo.InvariantCulture));
                return;
            }
            
            if (! BinaryFileManager.ProcessRequest(request))
            {
                Log.Debug("Url {0} not found. Returning 404 Not Found.", urlPath);
                response.StatusCode = 404;
                response.SuppressContent = true;
                application.CompleteRequest();
                Log.Trace(start, "binary-not-found", response.StatusCode.ToString(CultureInfo.InvariantCulture));
                return;
            }
            // if we got here, the file was successfully created on file-system
            DateTime ifModifiedSince = Convert.ToDateTime(request.Headers["If-Modified-Since"]);
            Log.Debug("If-Modified-Since: " + ifModifiedSince);

            DateTime fileLastModified = File.GetLastWriteTime(request.PhysicalPath);
            Log.Debug("File last modified: " + fileLastModified);

            if (fileLastModified.Subtract(ifModifiedSince).TotalSeconds < 1)
            {
                Log.Debug("Sending 304 Not Modified.");
                response.StatusCode = 304;
                response.SuppressContent = true;
                application.CompleteRequest();
                Log.Trace(start, "binary-not-modified", response.StatusCode.ToString(CultureInfo.InvariantCulture));
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
                    //Log.Error("IOException occurred while transmitting file: {0}\r\n{1}", ex.Message, ex.StackTrace);

                    // TODO: when file is accessed by a different thread, should we wait or just log an error
                    // file is accessed by a different thread (probabaly written), lets wait a second and try again?
                    Log.Warn("IOException transmitting file: {0}", ex.Message);
                    System.Threading.Thread.Sleep(1000);
                    response.TransmitFile(request.PhysicalPath);
                }
                Log.Trace(start, "binary-direct", response.StatusCode.ToString(CultureInfo.InvariantCulture));
                return;
            }

            Log.Trace(start, "binary-processed", response.StatusCode.ToString(CultureInfo.InvariantCulture));
        }

        public static void DistributionModule_OnBeginRequest(Object source, EventArgs e)
        {
            DateTime timer = DateTime.Now;            
            HttpContext context = HttpContext.Current;
            HttpRequest request = context.Request;
            string urlPath = request.Url.AbsolutePath;
            Log.Debug(">>DistributionModule_OnBeginRequest ({0})", urlPath);            
            if (!IsBinaryUrl.IsMatch(urlPath))
            {
                Log.Debug("Url {0} does not match binary url pattern, ignoring it.", urlPath);
                Log.Debug("<<DistributionModule_OnBeginRequest ({0})", urlPath);
                Log.Trace(timer, "binary-skip", "");
                return;
            }

            string realPath = request.PhysicalApplicationPath + SiteConfiguration.StaticsFolder + request.Path.Replace("/", "\\"); // request.PhysicalPath;
            context.RewritePath("/" + SiteConfiguration.StaticsFolder + request.Path);
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
                    CreateEmptyFile(realPath);
                }
                catch (Exception ex)
                {
                    Log.Warn("IIS empty file could not be created. {0}", ex.Message);
                }
            }

            Log.Debug("<<DistributionModule_OnBeginRequest ({0})", urlPath);
        }

		/// <summary>
		/// Do nothing
		/// </summary>
		public void Dispose() { }

        #endregion

        #region private
        private static void CreateEmptyFile(string filename)
        {
            //using (FileStream file = File.Create(filename))
            //using (StreamWriter sw = new StreamWriter(file))
            //{
            //    sw.Write(String.Empty);
            //    sw.Close();
            //    file.Close();
            //}
            File.Create(filename).Dispose();
        }

        private IBinaryFileManager _binaryFileManager;
        public virtual IBinaryFileManager BinaryFileManager 
        {
            get
            {
                return _binaryFileManager ?? (_binaryFileManager = new BinaryFileManager());
            }
            set
            {
                _binaryFileManager = value;
            }
        }

        private static Regex _isBinaryUrl;
        private static Regex IsBinaryUrl
        {
            get
            {
                return _isBinaryUrl ?? (_isBinaryUrl = new Regex(SiteConfiguration.MediaUrlRegex));
            }
        }

        #endregion
    }
}
