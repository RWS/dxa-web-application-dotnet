using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;

namespace Sdl.Web.Site.Areas.Core
{
    public class CoreAreaRegistration : BaseAreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Core";
            }
        }

        protected override void RegisterAllViewModels()
        {
            // Entity Views
            RegisterViewModel("LanguageSelector", typeof(Configuration));
            RegisterViewModel("Teaser-ImageOverlay", typeof(Teaser));
            RegisterViewModel("Teaser", typeof(Teaser));
            RegisterViewModel("TeaserColored", typeof(Teaser));
            RegisterViewModel("TeaserHero-ImageOverlay", typeof(Teaser));
            RegisterViewModel("TeaserMap", typeof(Teaser));

            RegisterViewModel("List", typeof(ContentList<Teaser>), "List");
            RegisterViewModel("PagedList", typeof(ContentList<Teaser>), "List");
            RegisterViewModel("ThumbnailList", typeof(ContentList<Teaser>), "List");

            RegisterViewModel("Breadcrumb", typeof(NavigationLinks), "Navigation");
            RegisterViewModel("LeftNavigation", typeof(NavigationLinks), "Navigation");
            RegisterViewModel("SiteMap", typeof(SitemapItem), "Navigation");
            RegisterViewModel("SiteMapXml", typeof(SitemapItem), "Navigation");
            RegisterViewModel("TopNavigation", typeof(NavigationLinks), "Navigation");

            // Page Views
            RegisterViewModel("GeneralPage", typeof(PageModel));
            RegisterViewModel("IncludePage", typeof(PageModel));
            RegisterViewModel("RedirectPage", typeof(PageModel));

            // Region Views
            RegisterViewModel("2-Column", typeof(RegionModel));
            RegisterViewModel("3-Column", typeof(RegionModel));
            RegisterViewModel("4-Column", typeof(RegionModel));
            RegisterViewModel("Hero", typeof(RegionModel));
            RegisterViewModel("Info", typeof(RegionModel));
            RegisterViewModel("Left", typeof(RegionModel));
            RegisterViewModel("Links", typeof(RegionModel));
            RegisterViewModel("Logo", typeof(RegionModel));
            RegisterViewModel("Main", typeof(RegionModel));
            RegisterViewModel("Nav", typeof(RegionModel));
            RegisterViewModel("Tools", typeof(RegionModel));

            // Region Views for Include Pages
            RegisterViewModel("Header", typeof(RegionModel));
            RegisterViewModel("Footer", typeof(RegionModel));
            RegisterViewModel("Left Navigation", typeof(RegionModel));
            RegisterViewModel("Content Tools", typeof(RegionModel));
        }
    }
}
