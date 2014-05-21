using Sdl.Web.DD4T;

namespace Site.Controllers
{
    public class PageController : DD4TController
    {
        public PageController()
        {
            ContentProvider = new DD4TContentProvider();
            Renderer = new DD4TRenderer();
        }
    }
}
