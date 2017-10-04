using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Tridion.ContentDelivery.DynamicContent;
using Tridion.ContentDelivery.Meta;
using Image = System.Drawing.Image; // TODO: Shouldn't use System.Drawing namespace in a web application.

namespace Sdl.Web.Tridion.Statics
{
    /// <summary>
    /// Ensures a Binary file is cached on the file-system from the Tridion Broker DB
    /// </summary>
    public class BinaryFileManager
    {
        private static readonly BinaryFileManager _instance = new BinaryFileManager();

        #region Inner classes
        internal class Dimensions
        {
            internal int Width; 
            internal int Height; 
            internal bool NoStretch;

            /// <summary>
            /// Returns a string that represents the current object.
            /// </summary>
            /// <returns>
            /// A string that represents the current object.
            /// </returns>
            public override string ToString()
            {
                return string.Format("(W={0}, H={1}, NoStretch={2})", Width, Height, NoStretch);
            }
        }
        #endregion

        /// <summary>
        /// Gets the singleton BinaryFileManager instance.
        /// </summary>
        internal static BinaryFileManager Instance
        {
            get
            {
                return _instance;
            }
        }

        private static BinaryMeta GetBinaryMeta(string urlPath, int publicationId)
        {
            BinaryMetaFactory binaryMetaFactory = new BinaryMetaFactory();
            return binaryMetaFactory.GetMetaByUrl(publicationId, urlPath);
        }

        private static DateTime GetBinaryLastPublishDate(string urlPath, int publicationId)
        {
            BinaryMeta binaryMeta = GetBinaryMeta(urlPath, publicationId);
            if (binaryMeta == null || !binaryMeta.IsComponent)
            {
                return DateTime.MinValue;
            }
            ComponentMetaFactory componentMetaFactory = new ComponentMetaFactory(publicationId);
            IComponentMeta componentMeta = componentMetaFactory.GetMeta(binaryMeta.Id);
            return componentMeta.LastPublicationDate;
        }

        /// <summary>
        /// Gets the cached local file for a given URL path.
        /// </summary>
        /// <param name="urlPath">The URL path.</param>
        /// <param name="localization">The Localization.</param>
        /// <returns>The path to the local file.</returns>
        internal string GetCachedFile(string urlPath, Localization localization)
        {
            string localFilePath = GetFilePathFromUrl(urlPath, localization);
            using (new Tracer(urlPath, localization, localFilePath))
            {
                Dimensions dimensions;
                urlPath = StripDimensions(urlPath, out dimensions);
                int publicationId = Convert.ToInt32(localization.LocalizationId);

                if (File.Exists(localFilePath))
                {
                    DateTime lastPublishedDate = SiteConfiguration.CacheProvider.GetOrAdd(
                        urlPath,
                        CacheRegions.BinaryPublishDate,
                        () => GetBinaryLastPublishDate(urlPath, publicationId)
                        );

                    if (localization.LastRefresh.CompareTo(lastPublishedDate) < 0)
                    {
                        //File has been modified since last application start but we don't care
                        Log.Debug(
                            "Binary with URL '{0}' is modified, but only since last application restart, so no action required",
                            urlPath);
                        return localFilePath;
                    }
                    FileInfo fi = new FileInfo(localFilePath);
                    if (fi.Length > 0)
                    {
                        DateTime fileModifiedDate = File.GetLastWriteTime(localFilePath);
                        if (fileModifiedDate.CompareTo(lastPublishedDate) >= 0)
                        {
                            Log.Debug("Binary with URL '{0}' is still up to date, no action required", urlPath);
                            return localFilePath;
                        }
                    }
                }
                // Binary does not exist or cached binary is out-of-date
                BinaryMeta binaryMeta = GetBinaryMeta(urlPath, publicationId);
                if (binaryMeta == null)
                {
                    // Binary does not exist in Tridion, it should be removed from the local file system too
                    if (File.Exists(localFilePath))
                    {
                        CleanupLocalFile(localFilePath);
                    }
                    throw new DxaItemNotFoundException(urlPath, localization.LocalizationId);
                }
                BinaryFactory binaryFactory = new BinaryFactory();
                BinaryData binaryData = binaryFactory.GetBinary(publicationId, binaryMeta.Id, binaryMeta.VariantId);

                WriteBinaryToFile(binaryData.Bytes, localFilePath, dimensions);
                return localFilePath;
            }
        }

        private static string GetFilePathFromUrl(string urlPath, Localization loc)
        {
            // TODO: not nice to use HttpContext at this level
            HttpContext httpContext = HttpContext.Current;
            return (httpContext == null) ?
                string.Format("{0}/{1}", SiteConfiguration.GetLocalStaticsFolder(loc.LocalizationId), Uri.UnescapeDataString(urlPath)) :
                httpContext.Server.MapPath("~/" + SiteConfiguration.GetLocalStaticsFolder(loc.LocalizationId) + urlPath);
        }

        /// <summary>
        /// Perform actual write of binary content to file
        /// </summary>
        /// <param name="binary">The binary to store</param>
        /// <param name="physicalPath">String the file path to write to</param>
        /// <param name="dimensions">Dimensions of file</param>
        /// <returns>True is binary was written to disk, false otherwise</returns>
        private static bool WriteBinaryToFile(byte[] binary, String physicalPath, Dimensions dimensions)
        {
            using (new Tracer(binary, physicalPath, dimensions))
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

                    byte[] buffer = binary;
                    if (dimensions != null && (dimensions.Width > 0 || dimensions.Height > 0))
                    {
                        buffer = ResizeImage(buffer, dimensions, GetImageFormat(physicalPath));
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
        }

        private static void CleanupLocalFile(string physicalPath)
        {
            using (new Tracer(physicalPath))
            {
                try
                {
                    // file got unpublished
                    File.Delete(physicalPath);
                    NamedLocker.RemoveLock(physicalPath);
                }
                catch (IOException)
                {
                    // file probabaly accessed by a different thread in a different process
                    Log.Warn("Cannot delete '{0}'. This can happen sporadically, let the next thread handle this.", physicalPath);
                }
            }
        }

        private static byte[] ResizeImage(byte[] imageData, Dimensions dimensions, ImageFormat imageFormat)
        {
            using (new Tracer(imageData.Length, dimensions, imageFormat))
            {
                Image original = Image.FromStream(new MemoryStream(imageData));

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
                    return imageData;
                }
                Image imgPhoto;
                using (MemoryStream memoryStream = new MemoryStream(imageData))
                {
                    imgPhoto = Image.FromStream(memoryStream);
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
                Match match = re.Match(path);
                string dim = match.Groups[2].ToString();
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
