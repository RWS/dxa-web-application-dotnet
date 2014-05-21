using Sdl.Web.DD4T;

namespace Site.Controllers
{
    public class RegionController : DD4TController
    {
        public RegionController()
        {
            ContentProvider = new DD4TContentProvider();
            Renderer = new DD4TRenderer();
        }
    }
}
