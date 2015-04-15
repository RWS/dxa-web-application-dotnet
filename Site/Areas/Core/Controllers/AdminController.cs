using System.Web.Mvc;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Mvc;

namespace Sdl.Web.Site.Areas.Core.Controllers
{
    [RoutePrefix("admin")]
    public class AdminController : Controller
    {
        [Route("refresh")]
        [NoCache]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult Refresh()
        {
            //trigger a reload of config/resources/mappings
            SiteConfiguration.Refresh();
            return Redirect("~/");
        }
	}
}