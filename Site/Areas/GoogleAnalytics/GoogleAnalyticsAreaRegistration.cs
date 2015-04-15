using System.Web.Mvc;

namespace Sdl.Web.Site.Areas.GoogleAnalytics
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
                "Default_GoogleAnalytics", 
                "{controller}/{action}/{id}", 
                new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}