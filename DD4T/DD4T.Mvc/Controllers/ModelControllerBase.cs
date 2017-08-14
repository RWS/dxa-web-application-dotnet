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
    public abstract class ModelControllerBase : DD4TControllerBase, IPageController, IComponentPresentationController
    {
        protected IViewModelFactory ViewModelFactory;

        public ModelControllerBase(IPageFactory pageFactory, IComponentPresentationFactory componentPresentationFactory, ILogger logger, IDD4TConfiguration dd4tConfiguration, IViewModelFactory viewModelFactory) : base(pageFactory, componentPresentationFactory, logger, dd4tConfiguration)
        {
            this.ViewModelFactory = viewModelFactory;
        }

        [HandleError]
        /// <summary>
        /// Retrieve a page from the provider, convert it into an IViewModel and render it with the specified view
        /// </summary>
        /// <returns>MVC ActionResult</returns>
        public virtual ActionResult PageModel(string url)
        {
            url = AddWelcomePageToUrl(url);
            IPage page = GetPage(url);
            if (page == null) { throw new HttpException(404, "Page cannot be found"); }
            IViewModel pageViewModel = ViewModelFactory.BuildViewModel(page);
            return View(GetViewName(page), pageViewModel);
        }


        /// <summary>
        /// Read IViewModel representing a component from the RouteData and render it with the specified view
        /// </summary>
        /// <returns>MVC ActionResult</returns>
        public virtual ActionResult ComponentModel()
        {
            IViewModel viewModel = RouteData.Values["model"] as IViewModel;
            string view = RouteData.Values["view"] as string;
            return View(view, viewModel);
        }

        /// <summary>
        /// Create IPage from XML in the request and forward to the view
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
                    IPage page = this.PageFactory.GetIPageObject(pageXml);
                    if (page == null)
                    {
                        throw new ModelNotCreatedException("--unknown--");
                    }
                    IViewModel pageViewModel = ViewModelFactory.BuildViewModel(page);
                    return View(GetViewName(page), pageViewModel);
                }
            }
            catch (SecurityException se)
            {
                throw new HttpException(403, se.Message);
            }
        }
    }
}
