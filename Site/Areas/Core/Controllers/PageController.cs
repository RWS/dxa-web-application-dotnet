using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Mvc.Controllers;
using System.Web;
using System.Web.Mvc;

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
    }
}
