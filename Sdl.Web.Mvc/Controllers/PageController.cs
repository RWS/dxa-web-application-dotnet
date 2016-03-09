using System;
using System.Web;
using System.Web.Mvc;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Mvc.Formats;

namespace Sdl.Web.Mvc.Controllers
{
    public class PageController : BaseController
    {
        /// <summary>
        /// Given a page URL, load the corresponding Page Model, Map it to the View Model and render it. 
        /// Can return XML or JSON if specifically requested on the URL query string (e.g. ?format=xml). 
        /// </summary>
        /// <param name="pageUrl">The page URL path (unescaped).</param>
        /// <returns>Rendered Page View Model</returns>
        [FormatData]
        public virtual ActionResult Page(string pageUrl)
        {
            bool addIncludes = true;
            object addIncludesViewData;
            if (ViewData.TryGetValue(DxaViewDataItems.AddIncludes, out addIncludesViewData))
            {
                addIncludes = (bool) addIncludesViewData;
            }

            PageModel pageModel;
            try
            {
                pageModel = ContentProvider.GetPageModel(pageUrl, WebRequestContext.Localization, addIncludes);
            }
            catch (DxaItemNotFoundException ex)
            {
                Log.Warn(ex.Message);
                return NotFound();
            }

            SetupViewData(pageModel);
            PageModel model = (EnrichModel(pageModel) as PageModel) ?? pageModel;

            if (!string.IsNullOrEmpty(model.Id))
            {
                WebRequestContext.PageId = model.Id;
            }

            Log.Debug("Page Request for URL '{0}' maps to Model [{1}] with View '{2}'", pageUrl, model, model.MvcData.ViewName);

            return View(model.MvcData.ViewName, model);
        }

        /// <summary>
        /// Resolve a item ID into a url and redirect to that URL
        /// </summary>
        /// <param name="itemId">The ID of the Page to resolve.</param>
        /// <param name="localizationId">The context Localization in which to resolve.</param>
        /// <param name="defaultItemId">Optional ID of a Component to resolve in case the Page cannot be resolved.</param>
        /// <param name="defaultPath"></param>
        /// <returns>null - response is redirected if the URL can be resolved</returns>
        public virtual ActionResult Resolve(string itemId, int localizationId, string defaultItemId = null, string defaultPath = null)
        {
            var localization = SiteConfiguration.LocalizationResolver.GetLocalization(localizationId.ToString());

            string url = SiteConfiguration.LinkResolver.ResolveLink(string.Format("tcm:{0}-{1}-64", localizationId, itemId), localization: localization);
            if (url == null && defaultItemId != null)
            {
                url = SiteConfiguration.LinkResolver.ResolveLink(string.Format("tcm:{0}-{1}", localizationId, defaultItemId), localization: localization);
            }
            if (url == null)
            {
                url = String.IsNullOrEmpty(defaultPath) ? "/" : defaultPath;
            }
            return Redirect(url);
        }

        /// <summary>
        /// Render a file not found page
        /// </summary>
        /// <returns>404 page or HttpException if there is none</returns>
        [FormatData]
        public virtual ActionResult NotFound()
        {
            string notFoundPageUrl = WebRequestContext.Localization.Path + "/error-404"; // TODO TSI-775: No need to prefix with WebRequestContext.Localization.Path here (?)

            PageModel pageModel;
            try
            {
                pageModel = ContentProvider.GetPageModel(notFoundPageUrl, WebRequestContext.Localization);
            }
            catch (DxaItemNotFoundException ex)
            {
                Log.Error(ex);
                throw new HttpException(404, ex.Message);
            }

            SetupViewData(pageModel);
            ViewModel model = EnrichModel(pageModel) ?? pageModel;
            Response.StatusCode = 404;
            return View(model.MvcData.ViewName, model);
        }
        

        public ActionResult ServerError()
        {
            //For a server error, it may be that there is an issue with connectivity,
            //so we show a very plain page with no dependency on the Content Provider
            Response.StatusCode = 500;
            return View();
        }

        public ActionResult Blank()
        {
            //For Experience Manager se_blank.html can be completely empty, or a valid HTML page without actual content
            return Content(string.Empty);


        }

        /// <summary>
        /// Enriches all the Region/Entity Models embedded in the given Page Model.
        /// </summary>
        /// <param name="model">The Page Model to enrich.</param>
        /// <remarks>Used by <see cref="FormatDataAttribute"/> to get all embedded Models enriched without rendering any Views.</remarks>
        internal void EnrichEmbeddedModels(PageModel model)
        {
            using (new Tracer(model))
            {
                if (model == null)
                {
                    return;
                }

                foreach (RegionModel region in model.Regions)
                {
                    // NOTE: Currently not enriching the Region Model itself, because we don't support custom Region Controllers (yet).
                    for (int i = 0; i < region.Entities.Count; i++)
                    {
                        EntityModel entity = region.Entities[i];
                        if (entity == null || entity.MvcData == null)
                        {
                            continue;
                        }

                        EntityModel enrichedEntityModel;
                        try
                        {
                            enrichedEntityModel = EnrichEntityModel(entity);
                        }
                        catch (Exception ex)
                        {
                            // If there is a problem enriching an Entity, we replace it with an ExceptionEntity which holds the error details and carry on.
                            Log.Error(ex);
                            enrichedEntityModel = new ExceptionEntity(ex);
                        }
                        region.Entities[i] = enrichedEntityModel;
                    }
                }
            }
        }

    }
}
