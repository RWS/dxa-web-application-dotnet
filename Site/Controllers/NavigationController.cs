using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Sdl.Web.DD4T;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Models;
using Sdl.Web.Mvc.Mapping;

namespace Site.Controllers
{
    public class NavigationController : DD4TController
    {      
        private SitemapItem NavigationModel
        {
            get
            {
                //This is a temporary measure to cache the navigationModel per request to not retrieve and serialize 3 times per request. Comprehensice caching strategy pending
                if (HttpContext.Items["navigationModel"] == null) 
                {
                    string navigationJsonString = GetContentForPage(Configuration.LocalizeUrl("navigation.json"));
                    var navigationModel = new JavaScriptSerializer().Deserialize<SitemapItem>(navigationJsonString);
                    HttpContext.Items["navigationModel"] = navigationModel;
                }                
                return HttpContext.Items["navigationModel"] as SitemapItem;
            }
        }

        public NavigationController()
        {
            ContentProvider = new DD4TContentProvider();
            Renderer = new DD4TRenderer();
        }

        public virtual ActionResult TopNavigation()
        {
            return View(NavigationModel);
        }

        public virtual ActionResult LeftNavigation()
        {
            var model = new NavigationBuilder(Request.Url.LocalPath.ToString()).GetParentNode(NavigationModel);
            return View(model);
        }

        public virtual ActionResult Breadcrumb()
        {
            var model = new NavigationBuilder(Request.Url.LocalPath.ToString()).BuildBreadcrumb(NavigationModel);
            return View(model);
        }

        public virtual ActionResult GoogleSitemap()
        {
            return View(NavigationModel);
        }



    }
}
