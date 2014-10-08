
using Sdl.Web.Common.Configuration;
namespace Sdl.Web.Common.Interfaces
{
    public interface IStaticFileManager
    {
        string CreateStaticAssets(string applicationRoot);
        string Serialize(string url, Localization loc, bool returnContents = false );
    }
}
