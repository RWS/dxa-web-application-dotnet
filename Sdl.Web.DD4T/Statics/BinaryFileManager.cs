using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using DD4T.ContentModel;
using DD4T.ContentModel.Contracts.Caching;
using DD4T.ContentModel.Factories;
using DD4T.Factories;
using DD4T.Factories.Caching;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Mvc.Statics;
using DD4T.ContentModel.Exceptions;

namespace Sdl.Web.DD4T.Statics
{
    /// <summary>
    /// Ensures a Binary file is cached on the file-system from the Tridion Broker DB
    /// </summary>
    public class BinaryFileManager : BaseStaticFileManager, IBinaryFileManager
    {
        #region caching
        private ICacheAgent _cacheAgent;
        /// <summary>
        /// Get or set the CacheAgent
        /// </summary>  
        public ICacheAgent CacheAgent 
        {
            get
            {
                return _cacheAgent ?? (_cacheAgent = new DefaultCacheAgent());
            }
            set
            {
                _cacheAgent = value;
            }
        }
        public const string CacheKeyFormatBinary = "Binary_{0}";
        private static string GetCacheKey(string url)
        {
            return string.Format(CacheKeyFormatBinary, url);
        }
        #endregion

        #region IBinaryFileManager
        /// <summary>
        /// Main worker method reads binary from Broker and stores it in file-system
        /// </summary>
        /// <returns></returns>
        public bool ProcessRequest(HttpRequest request)
        {
            string urlPath = request.Url.AbsolutePath.Replace("/" + SiteConfiguration.GetLocalStaticsUrl(WebRequestContext.Localization.LocalizationId), String.Empty);
            string physicalPath = request.PhysicalPath;
            Log.Debug("Start processing " + urlPath);
            return ProcessUrl(urlPath, false, physicalPath);
        }

        public string GetStaticContent(string urlPath, Localization loc, bool cacheSinceLastRefresh = false)
        {
            if (ProcessUrl(urlPath, cacheSinceLastRefresh, null, loc))
            {
                var filePath = GetFilePathFromUrl(urlPath, loc);
                if (File.Exists(filePath))
                {
                    return Encoding.UTF8.GetString(File.ReadAllBytes(filePath));
                }
            }
            return null;
        }

        /// <summary>
        /// Main worker method reads binary from Broker and stores it in file-system
        /// </summary>
        /// <returns></returns>
        public bool ProcessUrl(string urlPath, bool cacheSinceLastRefresh = false, string physicalPath = null, Localization loc = null)
        {
            Dimensions dimensions;
            if (loc==null)
            {
                loc = WebRequestContext.Localization;
            }
            bool connectionError = false;
            if (physicalPath == null)
            {
                physicalPath = GetFilePathFromUrl(urlPath, loc);
            }
            urlPath = StripDimensions(urlPath, out dimensions);
            string cacheKey = GetCacheKey(urlPath);
            DateTime? lastPublishedDate = CacheAgent.Load(cacheKey) as DateTime?;
            if (lastPublishedDate == null)
            {
                BinaryFactory.BinaryProvider.PublicationId = Int32.Parse(loc.LocalizationId);
                try
                {
                    DateTime lpb = BinaryFactory.FindLastPublishedDate(urlPath);
                    if (lpb != DateTime.MinValue.AddSeconds(1)) // this is the secret code for 'does not exist'
                    {
                        lastPublishedDate = lpb;
                        CacheAgent.Store(cacheKey, "Binary", lastPublishedDate);
                    }
                }
                catch (NullReferenceException)
                {
                    //Binary not found, this should return a min date, but theres a bug in DD4T where it throws a NRE
                    //DO NOTHING - binary removed later
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    connectionError = true; 
                }
            }
            if (connectionError)
            {
                if (File.Exists(physicalPath))
                {
                    //if theres an error connecting, but we still have a version on disk, serve this
                    return true;
                }
                return false;
            }
            if (lastPublishedDate != null)
            {
                if (File.Exists(physicalPath))
                {
                    if (cacheSinceLastRefresh && SiteConfiguration.LocalizationManager.GetLastLocalizationRefresh(loc.LocalizationId).CompareTo(lastPublishedDate) < 0)
                    {
                        //File has been modified since last application start but we don't care
                        Log.Debug("Binary {0} is modified, but only since last application restart, so no action required", urlPath);
                        return true;
                    }
                    FileInfo fi = new FileInfo(physicalPath);
                    if (fi.Length > 0)
                    {
                        DateTime fileModifiedDate = File.GetLastWriteTime(physicalPath);
                        if (fileModifiedDate.CompareTo(lastPublishedDate) >= 0)
                        {
                            Log.Debug("Binary {0} is still up to date, no action required", urlPath);
                            return true;
                        }
                    }
                }
            }

            // the normal situation (where a binary is still in Tridion and it is present on the file system already and it is up to date) is now covered
            // Let's handle the exception situations. 
            IBinary binary = GetBinaryFromBroker(urlPath,loc);
            if (binary==null)
            {
                Log.Debug("Binary with url {0} not found in publication {1}", urlPath, loc.LocalizationId);
                // binary does not exist in Tridion, it should be removed from the local file system too
                if (File.Exists(physicalPath))
                {
                    DeleteFile(physicalPath);
                }
                return false;
            }
            return WriteBinaryToFile(binary, physicalPath, dimensions);

        }

        private static string GetFilePathFromUrl(string urlPath, Localization loc)
        {
            return HttpContext.Current.Server.MapPath("~/" + SiteConfiguration.GetLocalStaticsFolder(loc.LocalizationId) + urlPath);
        }
        #endregion

        #region inner class Dimensions
        internal class Dimensions
        {
            internal int Width; internal int Height; internal bool NoStretch;
        }
        #endregion

        #region private


        private IBinaryFactory _binaryFactory;
        public virtual IBinaryFactory BinaryFactory
        {
            get
            {
                return _binaryFactory ?? (_binaryFactory = new BinaryFactory());
            }
            set
            {
                _binaryFactory = value;
            }
        }

        /// <summary>
        /// Perform actual write of binary content to file
        /// </summary>
        /// <param name="binary">The binary to store</param>
        /// <param name="physicalPath">String the file path to write to</param>
        /// <param name="dimensions">Dimensions of file</param>
        /// <returns>True is binary was written to disk, false otherwise</returns>
        private static bool WriteBinaryToFile(IBinary binary, String physicalPath, Dimensions dimensions)
        {
            bool result = true;
            try
            {
                if (!File.Exists(physicalPath))
                {
                    FileInfo fileInfo = new FileInfo(physicalPath);
                    if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
                    {
                        fileInfo.Directory.Create();
                    }
                }

                byte[] buffer = binary.BinaryData;
                if (dimensions != null && (dimensions.Width > 0 || dimensions.Height > 0))
                {
                    buffer = ResizeImageFile(buffer, dimensions, GetImageFormat(physicalPath));
                }

                lock (NamedLocker.GetLock(physicalPath))
                {
                    using (FileStream fileStream = new FileStream(physicalPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
                    {
                        fileStream.Write(buffer, 0, buffer.Length);                        
                    }
                }
            }
            catch (IOException)
            {
                // file probabaly accessed by a different thread in a different process, locking failed
                Log.Warn("Cannot write to {0}. This can happen sporadically, let the next thread handle this.", physicalPath);
                result = false;
            }

            return result;
        }

        private static void DeleteFile(string physicalPath)
        {
            if (File.Exists(physicalPath))
            {
                Log.Debug("Requested binary {0} no longer exists in Broker. Removing...", physicalPath);
                try
                {
                    // file got unpublished
                    File.Delete(physicalPath);
                    NamedLocker.RemoveLock(physicalPath);
                }
                catch (IOException)
                {
                    // file probabaly accessed by a different thread in a different process
                    Log.Warn("Cannot delete {0}. This can happen sporadically, let the next thread handle this.", physicalPath);
                }
                Log.Debug("Done ({0})", physicalPath);
            }
        }

        protected IBinary GetBinaryFromBroker(string urlPath, Localization loc)
        {
            IBinary binary = null;
            BinaryFactory.BinaryProvider.PublicationId = Int32.Parse(loc.LocalizationId);
            try
            {
                BinaryFactory.TryFindBinary(urlPath, out binary);
            }
            catch (BinaryNotFoundException ex)
            {
                Log.Warn("Binary: {0} does not exist", urlPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not load binary: {0}", urlPath);
            }
            //For some reason DD4T sometimes returns a non-null binary with null binary data if it doesnt exist
            return binary.BinaryData==null ? null : binary;
        }

        #endregion

        public override string Serialize(string url, Localization loc, bool returnContents = false)
        {
            if (returnContents)
            {
                return GetStaticContent(url, loc);
            }
            ProcessUrl(url, true, null, loc);
            return null;
        }

        internal static byte[] ResizeImageFile(byte[] imageFile, Dimensions dimensions, ImageFormat imageFormat)
        {
            Image original = Image.FromStream(new MemoryStream(imageFile));
            
            //Defaults for crop position, width and target size
            int cropX = 0, cropY = 0;
            int sourceW = original.Width, sourceH = original.Height;
            int targetW = original.Width, targetH = original.Height;
            
            //Most complex case is if a height AND width is specified
            if (dimensions.Width > 0 && dimensions.Height > 0)
            {
                if (dimensions.NoStretch)
                {
                    //If we don't want to stretch, then we crop
                    float originalAspect = (float)original.Width / (float)original.Height;
                    float targetAspect = (float)dimensions.Width / (float)dimensions.Height;
                    if (targetAspect < originalAspect)
                    {
                        //Crop the width - ensuring that we do not stretch if the requested height is bigger than the original
                        targetH = dimensions.Height > original.Height ? original.Height : dimensions.Height;
                        targetW = (int)Math.Ceiling(targetH * targetAspect);
                        cropX = (int)Math.Ceiling((original.Width - (original.Height * targetAspect)) / 2);
                        sourceW = sourceW - 2 * cropX;
                    }
                    else
                    {
                        //Crop the height - ensuring that we do not stretch if the requested width is bigger than the original
                        targetW = dimensions.Width > original.Width ? original.Width : dimensions.Width;
                        targetH = (int)Math.Ceiling(targetW / targetAspect);
                        cropY = (int)Math.Ceiling((original.Height - (original.Width / targetAspect)) / 2);
                        sourceH = sourceH - 2 * cropY;
                    }
                }
                else
                {
                    //We stretch to fit the dimensions
                    targetH = dimensions.Height;
                    targetW = dimensions.Width;
                }
            }
            //If we simply have a certain width or height, its simple: We just use that and derive the other
            //dimension from the original image aspect ratio. We also check if the target size is bigger than
            //the original, and if we allow stretching.
            else if (dimensions.Width > 0)
            {
                targetW = (dimensions.NoStretch && dimensions.Width > original.Width) ? original.Width : dimensions.Width;
                targetH = (int)(original.Height * ((float)targetW / (float)original.Width));
            }
            else
            {
                targetH = (dimensions.NoStretch && dimensions.Height > original.Height) ? original.Height : dimensions.Height;
                targetW = (int)(original.Width * ((float)targetH / (float)original.Height));
            }
            if (targetW == original.Width && targetH == original.Height)
            {
                //No resize required
                return imageFile;
            }
            Image imgPhoto;
            using (MemoryStream memoryStream = new MemoryStream(imageFile))
            {
                imgPhoto = Image.FromStream(memoryStream);
            }
            if (imgPhoto == null)
            {
                throw new Exception("cannot read image, binary data may not represent an image");
            }
            // Create a new blank canvas.  The resized image will be drawn on this canvas.
            Bitmap bmPhoto = new Bitmap(targetW, targetH, PixelFormat.Format24bppRgb);
            Bitmap bmOriginal = new Bitmap(original);
            bmPhoto.SetResolution(72, 72);
            bmPhoto.MakeTransparent(bmOriginal.GetPixel(0, 0));
            Graphics grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.SmoothingMode = SmoothingMode.AntiAlias;
            grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;
            grPhoto.PixelOffsetMode = PixelOffsetMode.HighQuality;
            grPhoto.DrawImage(imgPhoto, new Rectangle(0, 0, targetW, targetH), cropX, cropY, sourceW, sourceH, GraphicsUnit.Pixel);
            // Save out to memory and then to a file.  We dispose of all objects to make sure the files don't stay locked.
            using (MemoryStream memoryStream = new MemoryStream())
            {
                bmPhoto.Save(memoryStream, imageFormat);
                original.Dispose();
                imgPhoto.Dispose();
                bmPhoto.Dispose();
                grPhoto.Dispose();
                return memoryStream.GetBuffer();
            }
        }

        private static ImageFormat GetImageFormat(string path)
        {
            switch (Path.GetExtension(path).ToLower())
            {
                case ".jpg":
                case ".jpeg":
                    return ImageFormat.Jpeg;
                case ".gif":
                    return ImageFormat.Gif;
                //case ".png":
                // use png as default
                default:
                    return ImageFormat.Png;
            }
        }

        private static string StripDimensions(string path, out Dimensions dimensions)
        {
            dimensions = new Dimensions();
            Regex re = new Regex(@"_(w(\d+))?(_h(\d+))?(_n)?\.");
            if (re.IsMatch(path))
            {
                var match = re.Match(path);
                var dim = match.Groups[2].ToString();
                if (!String.IsNullOrEmpty(dim))
                {
                    dimensions.Width = Convert.ToInt32(dim);
                }
                dim = match.Groups[4].ToString();
                if (!String.IsNullOrEmpty(dim))
                {
                    dimensions.Height = Convert.ToInt32(dim);
                }
                if(!String.IsNullOrEmpty(match.Groups[5].ToString()))
                {
                    dimensions.NoStretch = true;
                }
                return re.Replace(path, ".");
            }

            // TSI-417: unescape and only escape spaces
            path = WebUtility.UrlDecode(path);
            path = path.Replace(" ", "%20");
            return path;
        }
    }
}
