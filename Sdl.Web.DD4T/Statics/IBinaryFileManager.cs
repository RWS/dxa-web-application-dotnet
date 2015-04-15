using System.Web;

namespace Sdl.Web.DD4T.Statics
{
    public interface IBinaryFileManager
    {
        bool ProcessRequest(HttpRequest request);
    }
}