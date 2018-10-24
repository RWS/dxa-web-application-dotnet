using System;
using System.Web;
using System.Web.Mvc;
using Sdl.Web.Common;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Mvc.Formats;
using Sdl.Web.Mvc.OutputCache;

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
        [DxaOutputCache]
        [FormatData] // must come first in execution order before output cache      
        public virtual ActionResult Page(string pageUrl)
        {
            // The pageUrl parameter provided by ASP.NET MVC is relative to the Web App, but we need a server-relative (i.e. absolute) URL path.
            string absoluteUrlPath = Request.Url.AbsolutePath;

            using (new Tracer(pageUrl, absoluteUrlPath))
            {
                try
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
                        pageModel = ContentProvider.GetPageModel(absoluteUrlPath, WebRequestContext.Localization, addIncludes);
                    }
                    catch (DxaItemNotFoundException ex)
                    {
                        Log.Info(ex.Message);
                        return NotFound();
                    }

                    PageModelWithHttpResponseData pageModelWithHttpResponseData = pageModel as PageModelWithHttpResponseData;
                    pageModelWithHttpResponseData?.SetHttpResponseData(System.Web.HttpContext.Current.Response);

                    SetupViewData(pageModel);
                    PageModel model = (EnrichModel(pageModel) as PageModel) ?? pageModel;

                    WebRequestContext.PageModel = model;

                    MvcData mvcData = model.MvcData;
                    if (mvcData == null)
                    {
                        throw new DxaException($"Page Model [{model}] has no MVC data.");
                    }

                    Log.Debug("Page Request for URL path '{0}' maps to Model [{1}] with View '{2}'", absoluteUrlPath, model, mvcData.ViewName);

                    return View(mvcData.ViewName, model);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    return ServerError();
                }
            }
        }

        /// <summary>
        /// Resolve a item ID into a url and redirect to that URL
        /// </summary>
        /// <param name="itemId">The ID of the Page to resolve.</param>
        /// <param name="localizationId">The context Localization in which to resolve.</param>
        /// <param name="defaultItem">Optional ID of a Component to resolve in case the Page cannot be resolved.</param>
        /// <param name="defaultPath"></param>
        /// <returns>null - response is redirected if the URL can be resolved</returns>
        public virtual ActionResult Resolve(string itemId, int localizationId, string defaultItem = null, string defaultPath = null)
        {
            using (new Tracer(itemId, localizationId, defaultItem, defaultPath))
            {
                string url = null;
                if (!string.IsNullOrEmpty(itemId))
                {
                    url = SiteConfiguration.LinkResolver.ResolveLink($"tcm:{localizationId}-{itemId}-64");
                    if (url == null && defaultItem != null)
                    {
                        if (defaultItem.IsCmIdentifier())
                        {
                            // we need to resolve this cm uri
                            string defaultItemId = defaultItem.Split('-')[1];
                            url = SiteConfiguration.LinkResolver.ResolveLink($"tcm:{localizationId}-{defaultItemId}");
                        }
                        else
                        {
                            // must of already been resolved in the model building pipeline
                            url = defaultItem;
                        }
                    }
                }
                if (url == null)
                {
                    url = string.IsNullOrEmpty(defaultPath) ? "/" : defaultPath;
                }
                return Redirect(url);
            }
        }

        /// <summary>
        /// Render a file not found page
        /// </summary>
        /// <returns>404 page or HttpException if there is none</returns>
        [FormatData]
        public virtual ActionResult NotFound()
        {
            using (new Tracer())
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
                PageModel model = EnrichModel(pageModel) as PageModel ?? pageModel;

                WebRequestContext.PageModel = model;

                Response.StatusCode = 404;
                return View(model.MvcData.ViewName, model);
            }
        }
        
        public ActionResult ServerError()
        {
            using (new Tracer())
            {
                //For a server error, it may be that there is an issue with connectivity,
                //so we show a very plain page with no dependency on the Content Provider
                Response.StatusCode = 500;
                return View("ServerError");
            }
        }

        public ActionResult Blank()
        {
            using (new Tracer())
            {
                //For Experience Manager se_blank.html can be completely empty, or a valid HTML page without actual content
                return Content(string.Empty);
            }
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
                        if (entity?.MvcData == null)
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
