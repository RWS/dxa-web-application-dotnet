using System.Web.Mvc;

namespace Sdl.Web.Modules.Search
{
    public class SearchAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Search";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            //Default Route - required for sub actions (region/entity/navigation etc.)
            context.MapRoute(
                "Default_Search", 
                "{controller}/{action}/{id}",
                new { controller = "Search", action = "Entity", id = UrlParameter.Optional }
            );
        }
    }
}