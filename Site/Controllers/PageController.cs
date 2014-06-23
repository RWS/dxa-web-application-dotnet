using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Common;
using System;

namespace Site.Controllers
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
