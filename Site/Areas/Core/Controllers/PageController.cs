using Sdl.Web.Mvc;
using Sdl.Web.Common.Interfaces;

namespace Site.Areas.Core.Controllers
{
    public class PageController : BaseController
    {
        public PageController(IContentProvider contentProvider, IRenderer renderer)
        {
            ContentProvider = contentProvider;
            Renderer = renderer;
        }
    }
}
