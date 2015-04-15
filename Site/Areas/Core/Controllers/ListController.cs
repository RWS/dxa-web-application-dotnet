using Sdl.Web.Common.Interfaces;
using Sdl.Web.Mvc.Controllers;

namespace Sdl.Web.Site.Areas.Core.Controllers
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
