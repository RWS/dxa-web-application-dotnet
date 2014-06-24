using System.Web.Mvc;

namespace Site.Areas.Core
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

            //Google Site Map
            context.MapRoute(
                "sitemap",
                "sitemap",
                new { controller = "Navigation", action = "GoogleSitemap" }
            );

            //For resolving ids to urls
            context.MapRoute(
               "Resolve",
               "resolve/{*itemId}",
               new { controller = "Resolver", action = "Resolve" },
               new { itemId = @"^(.*)?$" }
            );
            //Tridion Page Route
            context.MapRoute(
               "TridionPage",
               "{*pageUrl}",
               new { controller = "Page", action = "Page" },
               new { pageId = @"^(.*)?$" }
            );
            
            //Default Route - required for sub actions (region/entity/navigation etc.)
            context.MapRoute(
                name: "Default_Core",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}