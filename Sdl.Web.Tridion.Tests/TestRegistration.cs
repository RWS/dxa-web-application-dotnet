using System;
using Sdl.Web.Common.Models;
using Sdl.Web.Tridion.Tests.Models;
using Sdl.Web.Tridion.Tests.Models.Topic;
using Sdl.Web.Common.Models.Entity;

namespace Sdl.Web.Tridion.Tests
{
    internal static class TestRegistration 
    {
        internal static void RegisterViewModels()
        {
            // Entity Views
            RegisterViewModel("Article", typeof(Article));
            RegisterViewModel("Image", typeof(Image));
            RegisterViewModel("Download", typeof(Download));
            RegisterViewModel("TestEntity", typeof(TestEntity));

            RegisterViewModel("LanguageSelector", typeof(Common.Models.Configuration));

            RegisterViewModel("Breadcrumb", typeof(NavigationLinks), "Navigation");
            RegisterViewModel("LeftNavigation", typeof(NavigationLinks), "Navigation");
            RegisterViewModel("SiteMap", typeof(SitemapItem), "Navigation");
            RegisterViewModel("SiteMapXml", typeof(SitemapItem), "Navigation");
            RegisterViewModel("TopNavigation", typeof(NavigationLinks), "Navigation");

            RegisterViewModel("PagedList", typeof(ContentList<Teaser>), "List");

            RegisterViewModel("Test:TSI1758Test", typeof(Tsi1758TestEntity));
            RegisterViewModel("Test:TSI1946Test", typeof(Tsi1946TestEntity));
            RegisterViewModel("Test:TSI811Test", typeof(Tsi811TestEntity));
            RegisterViewModel("MediaManager:imagedist", typeof(MediaManagerDistribution));
            RegisterViewModel("Test:TSI1757Test1", typeof(Tsi1757TestEntity1));
            RegisterViewModel("Test:TSI1757Test2", typeof(Tsi1757TestEntity2));
            RegisterViewModel("Test:TSI1757Test3", typeof(Tsi1757TestEntity3));
            RegisterViewModel("Test:CompLinkTest", typeof(CompLinkTest));
            RegisterViewModel("Test:TSI2316Test", typeof(Tsi2316TestEntity));
            RegisterViewModel("Test:TSI3010Test", typeof(Tsi3010TestEntity));

            GenericTopic.Register(); // Generic Topic Model

            // Strongly Typed Topic Models
            RegisterViewModel(typeof(TestStronglyTypedTopic));
            RegisterViewModel(typeof(TestSpecializedTopic));

            // Page Views
            RegisterViewModel("GeneralPage", typeof(PageModel));
            RegisterViewModel("IncludePage", typeof(PageModel));
            RegisterViewModel("RedirectPage", typeof(PageModel));

            RegisterViewModel("Test:SimpleTestPage", typeof(PageModel));
            RegisterViewModel("Test:TSI811TestPage", typeof(Tsi811PageModel));
            RegisterViewModel("Test:TSI2285TestPage", typeof(Tsi2285PageModel));

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

            RegisterViewModel("SmartTarget:SmartTargetRegion", typeof(SmartTargetRegion));
            RegisterViewModel("SmartTarget:2-Column", typeof(SmartTargetRegion));

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

            MvcData mvcData = new MvcData(viewName)
            {
                ControllerName = controllerName
            };
            ModelTypeRegistry.RegisterViewModel(mvcData, modelType);
        }


        /// <summary>
        /// Registers a View Model Type without associated View.
        /// </summary>
        private static void RegisterViewModel(Type modelType)
        {
            ModelTypeRegistry.RegisterViewModel(null, modelType);
        }
    }
}
