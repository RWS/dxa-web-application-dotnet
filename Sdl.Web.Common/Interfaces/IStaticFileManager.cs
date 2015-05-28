
using System;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Common.Interfaces
{
    [Obsolete("Deprecated in DXA 1.1. We now use IContentProvider.GetStaticContentItem to get static content.")]
    public interface IStaticFileManager
    {
        [Obsolete("Not supported in DXA 1.1. Static assets are now lazy created/loaded on demand so this method is no longer required", error: true)]
        string CreateStaticAssets(string applicationRoot);
        string Serialize(string url, Localization loc, bool returnContents = true );
    }
}
