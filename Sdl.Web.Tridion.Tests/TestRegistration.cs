using System;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Tests
{
    internal static class TestRegistration 
    {
        internal static void RegisterCoreViewModels()
        {
            // Entity Views
            RegisterViewModel("HeaderLogo", typeof(Teaser));
            RegisterViewModel("LanguageSelector", typeof(Common.Models.Configuration));
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

        /// <summary>
        /// Registers a View Model and associated View.
        /// </summary>
        /// <param name="viewName">The name of the View to register.</param>
        /// <param name="modelType">The View Model Type to associate with the View. Must be a subclass of Type <see cref="ViewModel"/>.</param>
        /// <param name="controllerName">The Controller name. If not specified (or <c>null</c>), the Controller name is inferred from the <see cref="modelType"/>: either "Entity", "Region" or "Page".</param>
        private static void RegisterViewModel(string viewName, Type modelType, string controllerName = null)
        {
            if (String.IsNullOrEmpty(controllerName))
            {
                if (typeof(EntityModel).IsAssignableFrom(modelType))
                {
                    controllerName = "Entity";
                }
                else if (typeof(RegionModel).IsAssignableFrom(modelType))
                {
                    controllerName = "Region";
                }
                else
                {
                    controllerName = "Page";
                }
            }

            MvcData mvcData = new MvcData { AreaName = "Core", ControllerName = controllerName, ViewName = viewName };
            ModelTypeRegistry.RegisterViewModel(mvcData, modelType);
        }
    }
}
