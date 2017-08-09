using System.Web.Mvc;
using DD4T.ContentModel;
using DD4T.Mvc.Controllers;
using DD4T.Utils;
using DD4T.ContentModel.Exceptions;
using System.Text.RegularExpressions;
using System.Web.Routing;
using DD4T.ContentModel.Contracts.Logging;
using System;
using DD4T.ContentModel.Contracts.Configuration;

namespace DD4T.Mvc.Attributes
{
    /// <summary>
    /// Can be used to decorate hybrid controllers, which create their own model but need to have a Tridion page as additional input. The Tridion page is stored in ViewBag.Page and can be used in the view.
    /// </summary>
    public class UsesTridionPage : ActionFilterAttribute
    {
        private readonly ILogger _logger;
        public UsesTridionPage(ILogger logger, IDD4TConfiguration configuration)
        {
            if (logger == null)
                throw new ArgumentNullException("logger");
           

            _logger = logger;
        }
        /// <summary>
        /// Determines what to do if the page is not found. If the view relies on the page being there, this value should be true, otherwise it can be false.
        /// Defaults to true.
        /// </summary>
        public bool ThrowExceptionIfPageNotFound { get; set; }

        /// <summary>
        ///  Url of the page to retrieve (defaults to current url)
        /// </summary>
        public string PageUrl { get; set; }

        public UsesTridionPage()
        {
            ThrowExceptionIfPageNotFound = true;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!(filterContext.Controller is IPageController))
            {
                _logger.Warning("UsesTridionPage filter is used on a controller which does not implement IPageController");
                return;
            }

            string urlToLookUp = null;
            if (string.IsNullOrEmpty(PageUrl))
            {
                urlToLookUp = DD4T.Mvc.Html.TridionHelper.AddWelcomeFile(filterContext.RequestContext.HttpContext.Request.Path);
            }
            else
            {
                RouteData = filterContext.RouteData;
                urlToLookUp = ResolveRouteDataValues.Replace(PageUrl, new MatchEvaluator(ReplaceFromRouteData));
            }
            if (!urlToLookUp.StartsWith("/"))
                urlToLookUp = "/" + urlToLookUp;

            IPage page = null;
            if (((IPageController)filterContext.Controller).PageFactory.TryFindPage(urlToLookUp, out page))
            {
                filterContext.Controller.ViewBag.Page = page;
            }
            else
            {
                if (ThrowExceptionIfPageNotFound)
                    throw new PageNotFoundException();
            }
            base.OnActionExecuting(filterContext);
        }

        private string ReplaceFromRouteData(Match m)
        {
            return ((string)RouteData.Values[m.Groups[1].Captures[0].Value]) ?? "";
        }

        private RouteData RouteData { get; set; }

        private Regex _resolveRouteDataValues = null;
        private Regex ResolveRouteDataValues
        {
            get
            {
                if (_resolveRouteDataValues == null)
                    _resolveRouteDataValues = new Regex("\\{(.*?)\\}");
                return _resolveRouteDataValues;
            }
        }

    }
}
