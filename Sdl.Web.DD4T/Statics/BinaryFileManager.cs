using System;
using System.IO;
using System.Text;
using System.Web;
using DD4T.ContentModel;
using DD4T.ContentModel.Contracts.Caching;
using DD4T.ContentModel.Factories;
using DD4T.Factories;
using DD4T.Factories.Caching;
using DD4T.Utils;
using Sdl.Web.Mvc;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Sdl.Web.DD4T
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
                if (_cacheAgent == null)
                    _cacheAgent = new DefaultCacheAgent();
                return _cacheAgent;
            }
            set
            {
                _cacheAgent = value;
            }
        }
        public const string CacheKeyFormatBinary = "Binary_{0}";
        private string GetCacheKey(string url)
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
            string urlPath = request.Url.AbsolutePath.Replace("/" + Configuration.StaticsFolder, "");
            Log.Debug("Start processing " + urlPath);
            return ProcessUrl(urlPath);
        }


        public string GetStaticContent(string urlPath, bool cacheSinceAppStart = false)
        {
            if (ProcessUrl(urlPath, cacheSinceAppStart))
            {
                var filePath = GetFilePathFromUrl(urlPath);
                return Encoding.UTF8.GetString(File.ReadAllBytes(filePath));
            }
            else
            {
                return null;
            }
        }
            
        /// <summary>
        /// Main worker method reads binary from Broker and stores it in file-system
        /// </summary>
        /// <returns></returns>
        public bool ProcessUrl(string urlPath, bool cacheSinceAppStart = false)
        {
            Dimensions dimensions = null;
            String physicalPath = GetFilePathFromUrl(urlPath);
            urlPath = StripDimensions(urlPath, out dimensions);
            string cacheKey = GetCacheKey(urlPath);
            DateTime? lastPublishedDate = CacheAgent.Load(cacheKey) as DateTime?;
            if (lastPublishedDate == null)
            {
                BinaryFactory.BinaryProvider.PublicationId = GetLocalizationId(urlPath);
                DateTime lpb = BinaryFactory.FindLastPublishedDate(urlPath);
                if (lpb != DateTime.MinValue.AddSeconds(1)) // this is the secret code for 'does not exist'
                {
                    lastPublishedDate = lpb;
                    CacheAgent.Store(cacheKey, "Binary", lastPublishedDate);
                }
            }
            if (lastPublishedDate != null)
            {
                if (File.Exists(physicalPath))
                {
                    if (cacheSinceAppStart && Configuration.LastApplicationStart.CompareTo(lastPublishedDate) < 0)
                    {
                        //File has been modified since last application start but we don't care
                        Log.Debug("binary {0} is modified, but only since last application restart, so no action required", urlPath);
                        return true;
                    }
                    FileInfo fi = new FileInfo(physicalPath);
                    if (fi.Length > 0)
                    {
                        DateTime fileModifiedDate = File.GetLastWriteTime(physicalPath);
                        if (fileModifiedDate.CompareTo(lastPublishedDate) >= 0)
                        {
                            Log.Debug("binary {0} is still up to date, no action required", urlPath);
                            return true;
                        }
                    }
                }
            }

            // the normal situation (where a binary is still in Tridion and it is present on the file system already and it is up to date) is now covered
            // Let's handle the exception situations. 
            IBinary binary = GetBinaryFromBroker(urlPath);
            if (binary==null)
            {
                Log.Debug("Binary with url {0} not found", urlPath);
                // binary does not exist in Tridion, it should be removed from the local file system too
                if (File.Exists(physicalPath))
                {
                    DeleteFile(physicalPath);
                }
                return false;

            }
            return WriteBinaryToFile(binary, physicalPath, dimensions);

        }

        private string GetFilePathFromUrl(string urlPath)
        {
            return HttpContext.Current.Server.MapPath("~/" + Configuration.StaticsFolder + urlPath);
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
                if (_binaryFactory == null)
                {
                    _binaryFactory = new BinaryFactory();
                }
                return _binaryFactory;
            }
            set
            {
                _binaryFactory = value;
            }
        }

       
        /// <summary>
        /// Perform actual write of binary content to file
        /// </summary>
        /// <param name="binaryMeta">BinaryMeta the binary meta to store</param>
        /// <param name="physicalPath">String the file path to write to</param>
        /// <returns></returns>
        private bool WriteBinaryToFile(IBinary binary, String physicalPath, Dimensions dimensions)
        {
            bool result = true;
            FileStream fileStream = null;
            try
            {
                lock (physicalPath)
                {                    
                    if (File.Exists(physicalPath))
                    {
                        fileStream = new FileStream(physicalPath, FileMode.Create);
                    }
                    else
                    {
                        FileInfo fileInfo = new FileInfo(physicalPath);
                        if (!fileInfo.Directory.Exists)
                        {
                            fileInfo.Directory.Create();
                        }
                        fileStream = File.Create(physicalPath);
                    }
                    byte[] buffer = binary.BinaryData;

                    if (dimensions != null && (dimensions.Width>0 || dimensions.Height>0))
                        buffer = ResizeImageFile(buffer, dimensions, GetImageFormat(physicalPath));

                    fileStream.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception e)
            {
                Log.Error("Exception occurred {0}\r\n{1}", e.Message, e.StackTrace);
                result = false;
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }
            }

            return result;
        }

        private void DeleteFile(string physicalPath)
        {
            if (File.Exists(physicalPath))
            {
                Log.Debug("requested binary {0} no longer exists in broker. Removing...", physicalPath);
                File.Delete(physicalPath); // file got unpublished
                Log.Debug("done ({0})", physicalPath);
            }
        }

        protected int GetLocalizationId(string urlPath)
        {
            int localizationId = Int32.Parse(WebRequestContext.Localization.LocalizationId);
            if (localizationId == 0)
            {
                //When we are reading in config on application start, we cannot rely
                //On the publication resolver to get the right publication id, as there
                //is no HttpRequest to determine it from, so we match the binary url
                //with the configured localizations
                foreach (var loc in Configuration.Localizations.Values)
                {
                    if (urlPath.StartsWith(loc.Path))
                    {
                        localizationId = Int32.Parse(loc.LocalizationId);
                        break;
                    }
                }
            }
            return localizationId;
        }

        protected IBinary GetBinaryFromBroker(string urlPath)
        {
            IBinary binary;
            BinaryFactory.BinaryProvider.PublicationId = GetLocalizationId(urlPath);
            BinaryFactory.TryFindBinary(urlPath, out binary);
            return binary;
        }

        #endregion

        public override string Serialize(string url, bool returnContents = false)
        {
            if (returnContents)
            {
                return GetStaticContent(url, true);
            }
            else
            {
                ProcessUrl(url, true);
                return null;
            }
        }

        internal static byte[] ResizeImageFile(byte[] imageFile, Dimensions dimensions, ImageFormat imageFormat)
        {
            Image original = Image.FromStream(new MemoryStream(imageFile));
            
            //Defaults for crop position, width and target size
            int cropX = 0, cropY = 0;
            int sourceW = original.Width, sourceH = original.Height;
            int targetW = original.Width, targetH = original.Height;
            
            //Most complex case is if a height AND width is specified - as we will have to crop
            if (dimensions.Width > 0 && dimensions.Height > 0)
            {
                float originalAspect = (float)original.Width / (float)original.Height;
                float targetAspect = (float)dimensions.Width / (float)dimensions.Height;
                if (targetAspect < originalAspect)
                {
                    //We crop the width, but only if the required height is smaller than that of the original image
                    //Or we don't mind stretching the image to fit
                    if (dimensions.NoStretch == false || dimensions.Height <= original.Height)
                    {
                        targetH = dimensions.Height;
                        targetW = (int)Math.Ceiling(targetH * targetAspect);
                        cropX = (int)Math.Ceiling((original.Width - (original.Height * targetAspect)) / 2);
                        sourceW = sourceW - 2 * cropX;
                    }
                }
                else
                {
                    //We crop the height, but only if the required width is smaller than that of the original image
                    //Or we don't mind stretching the image to fit
                    if (dimensions.NoStretch == false || dimensions.Width <= original.Width)
                    {
                        targetW = dimensions.Width;
                        targetH = (int)Math.Ceiling(targetW / targetAspect);
                        cropY = (int)Math.Ceiling((original.Height - (original.Width / targetAspect)) / 2);
                        sourceH = sourceH - 2 * cropY;
                    }
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
            Image imgPhoto = null;
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
        private ImageFormat GetImageFormat(string path)
        {
            switch (Path.GetExtension(path).ToLower())
            {
                case ".jpg":
                case ".jpeg":
                    return ImageFormat.Jpeg;
                case ".png":
                    return ImageFormat.Png;
                case ".gif":
                    return ImageFormat.Gif;
            }
            return ImageFormat.Png; // use png as default
        }

        private string StripDimensions(string path, out Dimensions dimensions)
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
            return path;
        }
    }
}
