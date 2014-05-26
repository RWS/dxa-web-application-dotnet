using Sdl.Web.DD4T;
using Sdl.Web.Mvc;

namespace Site.Controllers
{
    public class PageController : BaseController
    {
        public PageController()
        {
            ContentProvider = new DD4TContentProvider();
            Renderer = new DD4TRenderer();
        }
    }
}
