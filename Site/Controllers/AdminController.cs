using System.Web.Mvc;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Configuration;

namespace Sdl.Web.Site.Controllers
{
    public class AdminController : Controller
    {
        [NoCache]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult Refresh()
        {
            //trigger a reload of config/resources/mappings
            WebRequestContext.Localization.Refresh(allSiteLocalizations: true);
            return Redirect("~" + WebRequestContext.Localization.Path + "/");
        }
	}
}