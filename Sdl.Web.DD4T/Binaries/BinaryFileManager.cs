using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DD4T.ContentModel.Contracts.Caching;
using System.Web;
using DD4T.ContentModel;
using DD4T.Utils;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using DD4T.Factories.Caching;
using DD4T.ContentModel.Exceptions;
using DD4T.ContentModel.Factories;
using DD4T.Factories;

namespace Sdl.Web.DD4T
{
    /// <summary>
    /// Ensures a Binary file is cached on the file-system from the Tridion Broker DB
    /// </summary>
    public class BinaryFileManager : IBinaryFileManager
    {
        #region caching
        private ICacheAgent _cacheAgent = null;
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
            string urlPath = request.Url.AbsolutePath.Replace("/BinaryData", "");
            LoggerService.Debug("Start processing " + urlPath);
            Dimensions dimensions = null;

            String physicalPath = request.PhysicalPath;
            string cacheKey = GetCacheKey(urlPath);
            DateTime? lastPublishedDate = CacheAgent.Load(cacheKey) as DateTime?;


            if (lastPublishedDate == null)
            {
                DateTime lpb = BinaryFactory.FindLastPublishedDate(urlPath);
                if (lpb != DateTime.MinValue.AddSeconds(1)) // this is the secret code for 'does not exist'
                {
                    lastPublishedDate = new DateTime?(lpb);
                    CacheAgent.Store(cacheKey, "Binary", lastPublishedDate);
                }
            }
            if (lastPublishedDate != null)
            {
                if (File.Exists(physicalPath))
                {
                    FileInfo fi = new FileInfo(physicalPath);
                    if (fi.Length > 0)
                    {

                        DateTime fileModifiedDate = File.GetLastWriteTime(physicalPath);
                        if (fileModifiedDate.CompareTo(lastPublishedDate) >= 0)
                        {
                            LoggerService.Debug("binary {0} is still up to date, no action required", urlPath);
                            return true;
                        }
                    }
                }
            }

            // the normal situation (where a binary is still in Tridion and it is present on the file system already and it is up to date) is now covered
            // Let's handle the exception situations. 
            IBinary binary = null;
            try
            {
                BinaryFactory.TryFindBinary(urlPath, out binary);
            }
            catch (BinaryNotFoundException)
            {
                LoggerService.Debug("Binary with url {0} not found", urlPath);
                // binary does not exist in Tridion, it should be removed from the local file system too
                if (File.Exists(physicalPath))
                {
                    DeleteFile(physicalPath);
                }
                return false;

            }
            return WriteBinaryToFile(binary, physicalPath, dimensions);

        }
        #endregion

        #region inner class Dimensions
        internal class Dimensions
        {
            internal int Width; internal int Height;
        }
        #endregion

        #region private


        private IBinaryFactory _binaryFactory = null;
        public virtual IBinaryFactory BinaryFactory
        {
            get
            {
                if (_binaryFactory == null)
                    _binaryFactory = new BinaryFactory();
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
        #endregion
    }
}
