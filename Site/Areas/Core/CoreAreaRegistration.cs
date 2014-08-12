using System.Web.Mvc;

namespace Sdl.Web.Site.Areas.Core
{
    public class CoreAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Core";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            //Navigation JSON
            context.MapRoute(
                "Core_Blank",
                "se_blank.html",
                new { controller = "Page", action = "Blank" }
            );
            //Navigation JSON
            context.MapRoute(
                "Core_Navigation",
                "navigation.json",
                new { controller = "Page", action = "PageRaw" }
            );
            //Navigation JSON
            context.MapRoute(
                "Core_Navigation_loc",
                "{localization}/navigation.json",
                new { controller = "Page", action = "PageRaw" }
            );
            //Google Site Map
            context.MapRoute(
                "Core_Sitemap",
                "sitemap.xml",
                new { controller = "Navigation", action = "SiteMap" }
            );
            //Google Site Map
            context.MapRoute(
                "Core_Sitemap_Loc",
                "{localization}/sitemap.xml",
                new { controller = "Navigation", action = "SiteMap" }
            );

            //For resolving ids to urls
            context.MapRoute(
               "Core_Resolve",
               "resolve/{*itemId}",
               new { controller = "Page", action = "Resolve" },
               new { itemId = @"^(.*)?$" }
            );

            //Tridion Page Route
            context.MapRoute(
               "Core_Page",
               "{*pageUrl}",
               new { controller = "Page", action = "Page" },
               new { pageId = @"^(.*)?$" }
            );
            
            //Default Route - required for sub actions (region/entity/navigation etc.)
            context.MapRoute(
                "Core_Default", 
                "{controller}/{action}/{id}", 
                new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}