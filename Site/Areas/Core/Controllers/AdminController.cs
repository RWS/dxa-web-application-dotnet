using System.Web;
using System.Web.Mvc;

namespace Site.Areas.Core.Controllers
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