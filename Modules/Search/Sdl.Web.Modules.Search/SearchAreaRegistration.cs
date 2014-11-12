using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;
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
            //This code block is for hybrid 1.0/2.0 compatibility 
            //for 2.0 this class should: 
            //1. Inherit from Sdl.Web.Mvc.Configuration.BaseAreaRegistration 
            //2. Use the following lines
            //RegisterViewModel("SearchBox", typeof(SearchConfiguration));
            //RegisterViewModel("SearchResults", typeof(SearchQuery<Teaser>), "Search");
            var mvcData = new MvcData { AreaName = "Search", ControllerName = "Entity", ViewName = "SearchBox" };
            SiteConfiguration.AddViewModelToRegistry(mvcData, @"~/Areas/Search/Views/Entity/SearchBox.cshtml");
            mvcData.ControllerName = "Search";
            mvcData.ViewName = "SearchResults";
            SiteConfiguration.AddViewModelToRegistry(mvcData, @"~/Areas/Search/Views/Search/SearchResults.cshtml");
            
        }
    }
}