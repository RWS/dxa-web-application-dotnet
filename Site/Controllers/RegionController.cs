using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Common;

namespace Site.Controllers
{
    public class RegionController : BaseController
    {
        public RegionController(IContentProvider contentProvider, IRenderer renderer)
        {
            ContentProvider = contentProvider;
            Renderer = renderer;
        }
    }
}
