
using Sdl.Web.Common.Configuration;
using System;
namespace Sdl.Web.Common.Interfaces
{
    public interface IStaticFileManager
    {
        [Obsolete("Static assets are now lazy created/loaded on demand so this method is no longer required", true)]
        string CreateStaticAssets(string applicationRoot);
        string Serialize(string url, Localization loc, bool returnContents = false );
    }
}
