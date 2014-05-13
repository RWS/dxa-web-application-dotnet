using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Sdl.Web.DD4T;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.Mvc.Models;

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
                    string navigationJsonString = this.GetContentForPage(Configuration.LocalizeUrl("navigation.json"));
                    var navigationModel = new JavaScriptSerializer().Deserialize<SitemapItem>(navigationJsonString);
                    HttpContext.Items["navigationModel"] = navigationModel;
                }                
                return HttpContext.Items["navigationModel"] as SitemapItem;
            }
        }

        public NavigationController()
        {
            this.ContentProvider = new DD4TModelFactory();
            this.Renderer = new DD4TRenderer();
        }

        public virtual ActionResult TopNavigation()
        {
            return View(NavigationModel);
        }

        public virtual ActionResult LeftNavigation()
        {
            //TODO: Filtering the Json here would help to pass only the part of the structure required to process. Tried Linq  with Newtonsoft JS.net and SelectToken without luck            
            //TODO: Caching Strategy
            return View(NavigationModel);
        }

        public virtual ActionResult Breadcrumb()
        {
            return View(NavigationModel);
        }

        public virtual ActionResult GoogleSitemap()
        {
            return View(NavigationModel);
        }



    }
}
