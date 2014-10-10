using System;
using System.Collections.Generic;
using System.Web.Helpers;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using System.IO;

namespace Sdl.Web.Mvc.Statics
{
    /// <summary>
    /// Class to manage static assets such as config and resources. This contains generic methods to serialize assets
    /// based on a root boostrap json file, which recursively loads in more assets. 
    /// Implementations of this class are responsible for implementing the Serialize method
    /// in order to read the static asset from somewhere (eg Broker DB) and serialize it to the file system
    /// </summary>
    public abstract class BaseStaticFileManager : IStaticFileManager
    {
        [Obsolete("Static assets are now lazy created/loaded on demand so this method is no longer required",true)]
        public virtual string CreateStaticAssets(string applicationRoot)
        {
            return null;
        }

        public abstract string Serialize(string url, Localization loc, bool returnContents = false);
    }
}
