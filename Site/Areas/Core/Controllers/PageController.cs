using System;
using System.Web;
using System.Web.Mvc;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Mvc.Controllers;
using Sdl.Web.Mvc.Formats;

namespace Sdl.Web.Site.Areas.Core.Controllers
{
    public class PageController : BaseController
    {
        /// <summary>
        /// Given a page URL, load the corresponding Page Model, Map it to the View Model and render it. 
        /// Can return XML or JSON if specifically requested on the URL query string (e.g. ?format=xml). 
        /// </summary>
        /// <param name="pageUrl">The page URL</param>
        /// <returns>Rendered Page View Model</returns>
        [FormatData]
        public virtual ActionResult Page(string pageUrl)
        {
            bool addIncludes = ViewBag.AddIncludes ?? true;
            PageModel pageModel = ContentProvider.GetPageModel(pageUrl, addIncludes);
            if (pageModel == null)
            {
                return NotFound();
            }

            SetupViewData(pageModel);
            PageModel model = (EnrichModel(pageModel) as PageModel) ?? pageModel;

            if (!string.IsNullOrEmpty(model.Id))
            {
                WebRequestContext.PageId = model.Id;
            }

            return View(model.MvcData.ViewName, model);
        }

        /// <summary>
        /// Resolve a item ID into a url and redirect to that URL
        /// </summary>
        /// <param name="itemId">The item id to resolve</param>
        /// <param name="localizationId">The site localization in which to resolve the URL</param>
        /// <param name="defaultItemId"></param>
        /// <param name="defaultPath"></param>
        /// <returns>null - response is redirected if the URL can be resolved</returns>
        public virtual ActionResult Resolve(string itemId, int localizationId, string defaultItemId = null, string defaultPath = null)
        {
            string url = SiteConfiguration.LinkResolver.ResolveLink("tcm:" + itemId, localizationId);
            if (url == null && defaultItemId != null)
            {
                url = SiteConfiguration.LinkResolver.ResolveLink("tcm:" + defaultItemId, localizationId);
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
            PageModel pageModel = ContentProvider.GetPageModel(WebRequestContext.Localization.Path + "/error-404");
            if (pageModel == null)
            {
                throw new HttpException(404, "Page Not Found");
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

    }
}
