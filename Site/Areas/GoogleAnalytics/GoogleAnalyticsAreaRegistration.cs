using System.Web.Mvc;

namespace Site.Areas.GoogleAnalytics
{
    public class GoogleAnalyticsAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "GoogleAnalytics";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            //Default Route - required for sub actions (region/entity/navigation etc.)
            context.MapRoute(
                name: "Default_GoogleAnalytics",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}