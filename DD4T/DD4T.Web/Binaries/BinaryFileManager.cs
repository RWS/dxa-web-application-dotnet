using System;
using System.Web;
using DD4T.ContentModel;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using DD4T.ContentModel.Exceptions;
using DD4T.ContentModel.Contracts.Caching;
using DD4T.ContentModel.Factories;
using System.Web.Mvc;
using DD4T.ContentModel.Contracts.Logging;

namespace DD4T.Web.Binaries
{
    /// <summary>
    /// Ensures a Binary file is cached on the file-system from the Tridion Broker DB
    /// </summary>
    public class BinaryFileManager : IBinaryFileManager
    {
        private readonly ICacheAgent CacheAgent;
        private readonly ILogger LoggerService;
        private readonly IBinaryFactory BinaryFactory;
       
        public BinaryFileManager()
        {
            //this is a wrong way of using your DI container. has to be done to support this Depracated class.
            CacheAgent = DependencyResolver.Current.GetService<ICacheAgent>();
            LoggerService = DependencyResolver.Current.GetService<ILogger>();
            BinaryFactory = DependencyResolver.Current.GetService<IBinaryFactory>();
        }
        #region caching
        

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
            string urlPath = request.Url.AbsolutePath.Replace("/BinaryData", "");

            LoggerService.Debug($"Start processing {urlPath} (physical path {request.PhysicalPath})");
            return BinaryFactory.FindAndStoreBinary(urlPath, request.PhysicalPath);

            //Dimensions dimensions = null;

            //urlPath = StripDimensions(urlPath, out dimensions);

            //String physicalPath = request.PhysicalPath;
            //string cacheKey = GetCacheKey(urlPath);
            //DateTime? lastPublishedDate = CacheAgent.Load(cacheKey) as DateTime?;


            //if (lastPublishedDate == null)
            //{
            //    DateTime lpb = BinaryFactory.FindLastPublishedDate(urlPath);
            //    if (lpb != DateTime.MinValue.AddSeconds(1)) // this is the secret code for 'does not exist'
            //    {
            //        lastPublishedDate = new DateTime?(lpb);
            //        CacheAgent.Store(cacheKey, "Binary", lastPublishedDate);
            //    }
            //}
            //if (lastPublishedDate != null)
            //{
            //    if (File.Exists(physicalPath))
            //    {
            //        FileInfo fi = new FileInfo(physicalPath);
            //        if (fi.Length > 0)
            //        {

            //            DateTime fileModifiedDate = File.GetLastWriteTime(physicalPath);
            //            if (fileModifiedDate.CompareTo(lastPublishedDate) >= 0)
            //            {
            //                LoggerService.Debug("binary {0} is still up to date, no action required", urlPath);
            //                return true;
            //            }
            //        }
            //    }
            //}

            //// the normal situation (where a binary is still in Tridion and it is present on the file system already and it is up to date) is now covered
            //// Let's handle the exception situations. 
            //IBinary binary = null;
            //try
            //{
            //    BinaryFactory.TryFindBinary(urlPath, out binary);
            //}
            //catch (BinaryNotFoundException)
            //{
            //    LoggerService.Debug("Binary with url {0} not found", urlPath);
            //    // binary does not exist in Tridion, it should be removed from the local file system too
            //    if (File.Exists(physicalPath))
            //    {
            //        DeleteFile(physicalPath);
            //    }
            //    return false;

            //}
            //return WriteBinaryToFile(binary, physicalPath, dimensions);

        }
        #endregion

        #region inner class Dimensions
        internal class Dimensions
        {
            internal int Width; internal int Height;
        }
        #endregion

        #region private


        

       
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

                    if (dimensions != null)
                        buffer = ResizeImageFile(buffer, dimensions, GetImageFormat(physicalPath));
                    fileStream.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception e)
            {
                LoggerService.Error("Exception occurred {0}\r\n{1}", e.Message, e.StackTrace);
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
                LoggerService.Debug("requested binary {0} no longer exists in broker. Removing...", physicalPath);
                File.Delete(physicalPath); // file got unpublished
                LoggerService.Debug("done ({0})", physicalPath);
            }
        }

        internal static byte[] ResizeImageFile(byte[] imageFile, Dimensions dimensions, ImageFormat imageFormat)
        {

            int targetH, targetW;
            Image original = Image.FromStream(new MemoryStream(imageFile));
            if (dimensions.Width > 0 && dimensions.Height > 0 && !(dimensions.Width == original.Width && dimensions.Height == original.Height))
            {
                targetW = dimensions.Width;
                targetH = dimensions.Height;
            }
            else if (dimensions.Width > 0 && dimensions.Width != original.Width)
            {
                targetW = dimensions.Width;
                targetH = (int)(original.Height * ((float)targetW / (float)original.Width));
            }
            else if (dimensions.Height > 0 && dimensions.Height != original.Height)
            {
                targetH = dimensions.Height;
                targetW = (int)(original.Width * ((float)targetH / (float)original.Height));
            }
            else
            {
                //No need to resize the image, return the original bytes.
                return imageFile;
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
            grPhoto.DrawImage(imgPhoto, new Rectangle(0, 0, targetW, targetH), 0, 0, original.Width, original.Height, GraphicsUnit.Pixel);
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
            Regex re = new Regex(@"_([hw])(\d+)\.");
            if (re.IsMatch(path))
            {
                string d = re.Match(path).Groups[1].ToString();
                string v = re.Match(path).Groups[2].ToString();
                if (d == "h")
                    dimensions = new Dimensions() { Height = Convert.ToInt32(v) };
                else
                    dimensions = new Dimensions() { Width = Convert.ToInt32(v) };
                return re.Replace(path, ".");
            }
            dimensions = null;
            return path;
        }
        #endregion
    }
}
