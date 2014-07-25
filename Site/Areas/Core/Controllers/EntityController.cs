using Sdl.Web.Common.Interfaces;
using Sdl.Web.Mvc.Controllers;

namespace Sdl.Web.Site.Areas.Core.Controllers
{
    public class EntityController : BaseController
    {
        public EntityController(IContentProvider contentProvider, IRenderer renderer)
        {
            ContentProvider = contentProvider;
            Renderer = renderer;
        }
    }
}
