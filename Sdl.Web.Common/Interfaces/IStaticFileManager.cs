
namespace Sdl.Web.Common.Interfaces
{
    public interface IStaticFileManager
    {
        string CreateStaticAssets(string applicationRoot);
        string Serialize(string url, bool returnContents = false );
    }
}
