using DD4T.ContentModel;
using DD4T.ContentModel.Contracts.Caching;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.ContentModel.Factories;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

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
            LoggerService = DependencyResolver.Current.GetService<ILogger>();
            BinaryFactory = DependencyResolver.Current.GetService<IBinaryFactory>();
        }

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
        }

        #endregion IBinaryFileManager
    }
}