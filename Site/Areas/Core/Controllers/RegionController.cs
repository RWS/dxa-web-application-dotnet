using Sdl.Web.Mvc;
using Sdl.Web.Common.Interfaces;

namespace Site.Areas.Core.Controllers
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
