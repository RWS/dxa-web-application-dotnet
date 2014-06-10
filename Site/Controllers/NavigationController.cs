using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Sdl.Web.DD4T;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Models;
using Sdl.Web.Mvc.Mapping;
using System;
using System.Collections.Generic;

namespace Site.Controllers
{
    public class NavigationController : BaseController
    {      
        private SitemapItem NavigationModel
        {
            get
            {
                //This is a temporary measure to cache the navigationModel per request to not retrieve and serialize 3 times per request. Comprehensice caching strategy pending
                if (HttpContext.Items["navigationModel"] == null) 
                {
                    string navigationJsonString = ContentProvider.GetPageContent(Configuration.LocalizeUrl("navigation.json"));
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
            ModelType = ModelType.Entity;
        }

        [HandleSectionError(View = "_SectionError")]
        public virtual ActionResult TopNavigation(object entity)
        {
            ViewBag.NavType = "Top";
            return Entity(entity);
        }

        [HandleSectionError(View = "_SectionError")]
        public virtual ActionResult LeftNavigation()
        {
            var model = new NavigationBuilder() { ContentProvider = this.ContentProvider, Sitemap = NavigationModel }.BuildContextNavigation(Request.Url.LocalPath.ToString());
            return View(model);
        }

        [HandleSectionError(View = "_SectionError")]
        public virtual ActionResult Breadcrumb()
        {
            var model = new NavigationBuilder() { ContentProvider = this.ContentProvider, Sitemap = NavigationModel }.BuildBreadcrumb(Request.Url.LocalPath.ToString());
            return View(model);
        }
        
        [HandleError]
        public virtual ActionResult GoogleSitemap()
        {
            return View(NavigationModel);
        }

        protected override object ProcessModel(object sourceModel, Type type, List<string> includes = null)
        {
            var model = base.ProcessModel(sourceModel, type, includes);
            var nav = model as NavigationLinks;
            string navType = ViewBag.NavType;
            NavigationLinks links = new NavigationLinks();
            switch (navType)
            {
                case "Top":
                    links = new NavigationBuilder() { ContentProvider = this.ContentProvider, Sitemap = NavigationModel }.BuildTopNavigation(Request.Url.LocalPath.ToString());
                    break;
            }
            if (nav!=null)
            {
                
                links.EntityData = nav.EntityData;
                links.PropertyData = nav.PropertyData;
            }
            return links;
        }
    }
}
