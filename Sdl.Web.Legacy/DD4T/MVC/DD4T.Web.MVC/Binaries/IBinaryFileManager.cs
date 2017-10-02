using System.Web;

namespace DD4T.Web.Binaries
{
    public interface IBinaryFileManager
    {
        bool ProcessRequest(HttpRequest request);
    }
}
