using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Mapping;
using System.Web;
using System.Web.Mvc;

namespace Sdl.Web.Site.Areas.Core.Controllers
{
    [RoutePrefix("admin")]
    public class AdminController : Controller
    {
        [Route("refresh")]
        public ActionResult Refresh()
        {
            //trigger a reload of config/resources/mappings
            SiteConfiguration.Refresh();
            return Redirect("~/");
        }
	}
}