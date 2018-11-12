using System.Collections.Generic;
using System.Web.Mvc;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Navigation;
using Sdl.Web.Mvc.Configuration;

namespace Sdl.Web.Mvc.Controllers
{
    public class NavigationController : BaseController
    {
        /// <summary>
        /// Populate and render a navigation entity model
        /// </summary>
        /// <param name="entity">The navigation entity</param>
        /// <param name="navType">The type of navigation to render</param>
        /// <param name="containerSize">The size (in grid units) of the container the navigation element is in</param>
        /// <returns></returns>
        [HandleSectionError(View = "SectionError")]
        public virtual ActionResult Navigation(EntityModel entity, string navType, int containerSize = 0)
        {
            using (new Tracer(entity, navType, containerSize))
            {
                SetupViewData(entity, containerSize);

                INavigationProvider navigationProvider = SiteConfiguration.NavigationProvider;
                string requestUrlPath = Request.Url.LocalPath;
                Localization localization = WebRequestContext.Localization;
                NavigationLinks model;
                switch (navType)
                {
                    case "Top":
                        model = navigationProvider.GetTopNavigationLinks(requestUrlPath, localization);
                        break;
                    case "Left":
                        model = navigationProvider.GetContextNavigationLinks(requestUrlPath, localization);
                        break;
                    case "Breadcrumb":
                        model = navigationProvider.GetBreadcrumbNavigationLinks(requestUrlPath, localization);
                        break;
                    default:
                        throw new DxaException("Unexpected navType: " + navType);
                }

                EntityModel sourceModel = (EnrichModel(entity) as EntityModel) ?? entity;
                model.XpmMetadata = sourceModel.XpmMetadata;
                model.XpmPropertyMetadata = sourceModel.XpmPropertyMetadata;

                return View(sourceModel.MvcData.ViewName, model);
            }
        }

        /// <summary>
        /// Retrieves a rendered HTML site map
        /// </summary>
        /// <param name="entity">The sitemap entity</param>
        /// <returns>Rendered site map HTML.</returns>
        public virtual ActionResult SiteMap(SitemapItem entity)
        {
            using (new Tracer(entity))
            {
                SitemapItem model = SiteConfiguration.NavigationProvider.GetNavigationModel(WebRequestContext.Localization);
                SetupViewData(entity);
                return View(entity.MvcData.ViewName, model);
            }
        }

        /// <summary>
        /// Retrieves a Google XML site map
        /// </summary>
        /// <returns>Google site map XML.</returns>
        public virtual ActionResult SiteMapXml()
        {
            using (new Tracer())
            {
                SitemapItem model = SiteConfiguration.NavigationProvider.GetNavigationModel(WebRequestContext.Localization);
                return View("SiteMapXml", model);
            }
        }

        /// <summary>
        /// Retrieves a JSON site map
        /// </summary>
        /// <returns>Site map JSON.</returns>
        public virtual ActionResult SiteMapJson()
        {
            using (new Tracer())
            {
                SitemapItem model = SiteConfiguration.NavigationProvider.GetNavigationModel(WebRequestContext.Localization);
                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }

        public virtual ActionResult GetNavigationSubtree(string sitemapItemId)
        {
            using (new Tracer(sitemapItemId))
            {
                NavigationFilter navFilter = new NavigationFilter
                {
                    IncludeAncestors = GetRequestParameter<bool>("includeAncestors")
                };

                int descendantLevels;
                if (TryGetRequestParameter("descendantLevels", out descendantLevels))
                {
                    navFilter.DescendantLevels = descendantLevels;
                }

                IOnDemandNavigationProvider onDemandNavigationProvider = SiteConfiguration.NavigationProvider as IOnDemandNavigationProvider;
                if (onDemandNavigationProvider == null)
                {
                    Log.Warn("Request for Navigation subtree received, but current Navigation Provider ({0}) does not implement interface {1}",
                        SiteConfiguration.NavigationProvider.GetType().Name, typeof(IOnDemandNavigationProvider).Name);
                    return new EmptyResult();
                }

                IEnumerable<SitemapItem> model = onDemandNavigationProvider.GetNavigationSubtree(sitemapItemId, navFilter, WebRequestContext.Localization);
                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
