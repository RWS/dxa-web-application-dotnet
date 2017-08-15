using System.Web.Mvc;
using DD4T.ContentModel;
using DD4T.ContentModel.Exceptions;
using DD4T.Mvc.Html;
using System.Web;
using System.IO;
using System.Security;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.ContentModel.Factories;
using System;
using DD4T.Core.Contracts.ViewModels;
using DD4T.ContentModel.Contracts.Configuration;

namespace DD4T.Mvc.Controllers
{
    public abstract class TridionControllerBase : DD4TControllerBase, IPageController, IComponentPresentationController
    {

        public TridionControllerBase(IPageFactory pageFactory, IComponentPresentationFactory componentPresentationFactory, ILogger logger, IDD4TConfiguration dd4tConfiguration) : base(pageFactory, componentPresentationFactory, logger, dd4tConfiguration)
        {
        }

        [HandleError]
        public virtual ActionResult Page(string url)
        {
            url = AddWelcomePageToUrl(url);
            IPage page = GetPage(url);
            if (page == null) { throw new HttpException(404, "Page cannot be found"); }
            return View(GetViewName(page), page);
        }

        /// <summary>
        /// Read component presentation from RouteData and render it with the specified view
        /// </summary>
        /// <param name="componentPresentationId"></param>
        /// <returns></returns>
        public virtual ActionResult ComponentPresentation() // TODO: overload with component/ct uri to retrieve DCPs
        {
            LoggerService.Information(">>ComponentPresentation", LoggingCategory.Performance);
            try
            {
                IComponentPresentation componentPresentation = RouteData.Values["ComponentPresentation"] as IComponentPresentation;
                if (componentPresentation == null)
                {
                    throw new ArgumentException("No ComponentPresentation found in the RouteData");
                }
                return View(GetViewName(componentPresentation), componentPresentation);
            }
            catch (ConfigurationException e)
            {
                ViewResult result = View("Configuration exception: " + e.Message);
                LoggerService.Information("<<ComponentPresentation", LoggingCategory.Performance);
                return result;
            }
        }


        /// <summary>
        /// Create IPage from data in the request and forward to the view
        /// </summary>
        /// <example>
        /// To use, add the following code to the Global.asax.cs of your MVC web application:
        ///             routes.MapRoute(
        ///                "PreviewPage",
        ///                "{*PageId}",
        ///                new { controller = "Page", action = "PreviewPage" }, // Parameter defaults
        ///                new { httpMethod = new HttpMethodConstraint("POST") } // Parameter constraints
        ///            );
        ///            * This is assuming that you have a controller called PageController which extends TridionControllerBase
        /// </example>
        /// <returns></returns>
        [HandleError]
        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateInput(false)]
        public System.Web.Mvc.ActionResult PreviewPage()
        {

            try
            {
                using (StreamReader reader = new StreamReader(this.Request.InputStream))
                {
                    string pageXml = reader.ReadToEnd();
                    IPage page = PageFactory.GetIPageObject(pageXml);
                    if (page == null)
                    {
                        throw new ModelNotCreatedException("--unknown--");
                    }
                    return View(GetViewName(page), page);
                }
            }
            catch (SecurityException se)
            {
                throw new HttpException(403, se.Message);
            }
        }



    }
}
