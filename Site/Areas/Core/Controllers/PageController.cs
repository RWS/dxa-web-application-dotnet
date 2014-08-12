using System.Web.Mvc;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Mvc.Controllers;

namespace Sdl.Web.Site.Areas.Core.Controllers
{
    public class PageController : BaseController
    {
        public PageController(IContentProvider contentProvider, IRenderer renderer)
        {
            ContentProvider = contentProvider;
            Renderer = renderer;
        }

        public ActionResult ServerError()
        {
            //For a server error, it may be that there is an issue with connectivity,
            //so we show a very plain page with no dependency on the Content Provider
            Response.StatusCode = 500;
            return View();
        }

        public ActionResult Blank()
        {
            //For Experience Manager se_blank.html can be completely empty, or a valid HTML page without actual content
            return Content(string.Empty);
        }

    }
}
