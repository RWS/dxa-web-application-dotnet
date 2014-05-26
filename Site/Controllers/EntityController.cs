using Sdl.Web.DD4T;
using Sdl.Web.Mvc;

namespace Site.Controllers
{
    public class EntityController : BaseController
    {
        public EntityController()
        {
            ContentProvider = new DD4TContentProvider();
            Renderer = new DD4TRenderer();
        }
    }
}
