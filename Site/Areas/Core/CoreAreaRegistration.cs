using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using System;
using System.IO;
using System.Web.Mvc;

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
            RegisterViewModel("GeneralPage", typeof(WebPage), "Page");
            RegisterViewModel("IncludePage", typeof(WebPage), "Page");
            RegisterViewModel("RedirectPage", typeof(WebPage), "Page");
            RegisterViewModel("2-Column", typeof(Region), "Region");
            RegisterViewModel("3-Column", typeof(Region), "Region");
            RegisterViewModel("4-Column", typeof(Region), "Region");
            RegisterViewModel("Hero", typeof(Region), "Region");
            RegisterViewModel("Info", typeof(Region), "Region");
            RegisterViewModel("Left", typeof(Region), "Region");
            RegisterViewModel("Links", typeof(Region), "Region");
            RegisterViewModel("Logo", typeof(Region), "Region");
            RegisterViewModel("Main", typeof(Region), "Region");
            RegisterViewModel("Nav", typeof(Region), "Region");
            RegisterViewModel("Tools", typeof(Region), "Region");
        }
        
    }
}