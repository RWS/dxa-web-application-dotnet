using Sdl.Web.Mvc;
using Sdl.Web.Common.Interfaces;

namespace Site.Areas.Core.Controllers
{
    public class ListController : BaseController
    {
        public ListController(IContentProvider contentProvider, IRenderer renderer)
        {
            ContentProvider = contentProvider;
            Renderer = renderer;
        }
    }
}
