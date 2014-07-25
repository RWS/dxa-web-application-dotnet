using System.Web;
using System.Web.Mvc;

namespace Sdl.Web.Site.Areas.Core.Controllers
{
    public class AdminController : Controller
    {
        [Route("restart")]
        [Authorize(Roles = "Administrator")]
        public ActionResult Restart()
        {
            HttpRuntime.UnloadAppDomain();
            return Redirect("~/");
        }
	}
}