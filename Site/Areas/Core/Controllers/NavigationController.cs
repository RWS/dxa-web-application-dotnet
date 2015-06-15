using System.Web.Mvc;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Mvc.Controllers;

namespace Sdl.Web.Site.Areas.Core.Controllers
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

        /// <summary>
        /// Retrieves a rendered HTML site map
        /// </summary>
        /// <param name="entity">The sitemap entity</param>
        /// <returns>Rendered site map HTML.</returns>
        public virtual ActionResult SiteMap(SitemapItem entity)
        {
            SitemapItem model = SiteConfiguration.NavigationProvider.GetNavigationModel(WebRequestContext.Localization);
            SetupViewData(entity);
            return View(entity.MvcData.ViewName, model);
        }

        /// <summary>
        /// Retrieves a Google XML site map
        /// </summary>
        /// <returns>Google site map XML.</returns>
        public virtual ActionResult SiteMapXml()
        {
            SitemapItem model = SiteConfiguration.NavigationProvider.GetNavigationModel(WebRequestContext.Localization);
            return View("SiteMapXml", model);
        }

        /// <summary>
        /// Retrieves a JSON site map
        /// </summary>
        /// <returns>Site map JSON.</returns>
        public virtual ActionResult SiteMapJson()
        {
            SitemapItem model = SiteConfiguration.NavigationProvider.GetNavigationModel(WebRequestContext.Localization);
            return Json(model, JsonRequestBehavior.AllowGet);
        }
    }
}
