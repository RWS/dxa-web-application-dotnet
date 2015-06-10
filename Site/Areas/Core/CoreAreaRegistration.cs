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
            RegisterViewModel("Accordion", typeof(ItemList));
            RegisterViewModel("Article", typeof(Article));
            RegisterViewModel("Carousel", typeof(ItemList));
            RegisterViewModel("CookieNotificationBar", typeof(Notification));
            RegisterViewModel("Download", typeof(Download));
            RegisterViewModel("FooterLinkGroup", typeof(LinkList<Link>));
            RegisterViewModel("FooterLinks", typeof(LinkList<Link>));
            RegisterViewModel("HeaderLinks", typeof(LinkList<Link>));
            RegisterViewModel("HeaderLogo", typeof(Teaser));
            RegisterViewModel("LanguageSelector", typeof(Configuration));
            RegisterViewModel("OldBrowserNotificationBar", typeof(Notification));
            RegisterViewModel("Place", typeof(Place));
            RegisterViewModel("SocialLinks", typeof(LinkList<TagLink>));
            RegisterViewModel("SocialSharing", typeof(LinkList<TagLink>));
            RegisterViewModel("Tab", typeof(ItemList));
            RegisterViewModel("Teaser-ImageOverlay", typeof(Teaser));
            RegisterViewModel("Teaser", typeof(Teaser));
            RegisterViewModel("TeaserColored", typeof(Teaser));
            RegisterViewModel("TeaserHero-ImageOverlay", typeof(Teaser));
            RegisterViewModel("TeaserMap", typeof(Teaser));
            RegisterViewModel("YouTubeVideo", typeof(YouTubeVideo));

            RegisterViewModel("List", typeof(ContentList<Teaser>), "List");
            RegisterViewModel("PagedList", typeof(ContentList<Teaser>), "List");
            RegisterViewModel("ThumbnailList", typeof(ContentList<Teaser>), "List");

            RegisterViewModel("Breadcrumb", typeof(NavigationLinks), "Navigation");
            RegisterViewModel("LeftNavigation", typeof(NavigationLinks), "Navigation");
            RegisterViewModel("SiteMap", typeof(SitemapItem), "Navigation");
            RegisterViewModel("SiteMapXml", typeof(SitemapItem), "Navigation");
            RegisterViewModel("TopNavigation", typeof(NavigationLinks), "Navigation");

            // Entity Models without view
            RegisterViewModel(typeof(Image));

            // Page Views
            RegisterViewModel("GeneralPage", typeof(PageModel), "Page");
            RegisterViewModel("IncludePage", typeof(PageModel), "Page");
            RegisterViewModel("RedirectPage", typeof(PageModel), "Page");

            // Region Views
            RegisterViewModel("2-Column", typeof(RegionModel), "Region");
            RegisterViewModel("3-Column", typeof(RegionModel), "Region");
            RegisterViewModel("4-Column", typeof(RegionModel), "Region");
            RegisterViewModel("Hero", typeof(RegionModel), "Region");
            RegisterViewModel("Info", typeof(RegionModel), "Region");
            RegisterViewModel("Left", typeof(RegionModel), "Region");
            RegisterViewModel("Links", typeof(RegionModel), "Region");
            RegisterViewModel("Logo", typeof(RegionModel), "Region");
            RegisterViewModel("Main", typeof(RegionModel), "Region");
            RegisterViewModel("Nav", typeof(RegionModel), "Region");
            RegisterViewModel("Tools", typeof(RegionModel), "Region");

            // Region Views for Include Pages
            RegisterViewModel("Header", typeof(RegionModel), "Region");
            RegisterViewModel("Footer", typeof(RegionModel), "Region");
            RegisterViewModel("Left Navigation", typeof(RegionModel), "Region");
            RegisterViewModel("Content Tools", typeof(RegionModel), "Region");
        }
    }
}
