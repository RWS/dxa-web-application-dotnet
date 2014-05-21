using Sdl.Web.DD4T;

namespace Site.Controllers
{
    public class EntityController : DD4TController
    {
        public EntityController()
        {
            ContentProvider = new DD4TContentProvider();
            Renderer = new DD4TRenderer();
        }
    }
}
