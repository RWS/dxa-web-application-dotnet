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
            //Default Route - required for sub actions (region/entity/navigation etc.)
            context.MapRoute(
                "Core_Default", 
                "{controller}/{action}/{id}", 
                new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}