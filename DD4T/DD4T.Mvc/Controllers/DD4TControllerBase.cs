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
using System.Text.RegularExpressions;

namespace DD4T.Mvc.Controllers
{
    public abstract class DD4TControllerBase : Controller
    {


        public IPageFactory PageFactory { get; set; }
        public IComponentPresentationFactory ComponentPresentationFactory { get; set; }
      
        protected IDD4TConfiguration DD4TConfiguration { get; set; }

        protected ILogger LoggerService { get; set; }

        public DD4TControllerBase(IPageFactory pageFactory, IComponentPresentationFactory componentPresentationFactory, ILogger logger, IDD4TConfiguration dd4tConfiguration)
        {
            if (pageFactory == null)
                throw new ArgumentNullException("pageFactory");

            if (componentPresentationFactory == null)
                throw new ArgumentNullException("componentPresentationFactory");

            if (logger == null)
                throw new ArgumentNullException("logger");

            LoggerService = logger;
            PageFactory = pageFactory;
            ComponentPresentationFactory = componentPresentationFactory;
            DD4TConfiguration = dd4tConfiguration;
        }

        protected IPage GetPage(string url)
        {
            IPage page;
            if (!url.StartsWith("/"))
            {
                url = "/" + url;
            }
            if (PageFactory != null)
            {               
                if (PageFactory.TryFindPage(url, out page))
                {
                    return page;
                }
            }
            else
                throw new ConfigurationException("No PageFactory configured");

            return page;
        }

        private Regex reDefaultPage = new Regex(@".*/[^/\.]*(/?)$");
        private string _welcomeFileName = null;
        public string WelcomeFileName
        {
            get
            {
                if (_welcomeFileName == null)
                {
                    _welcomeFileName = DD4TConfiguration.WelcomeFile;
                }
                return _welcomeFileName;
            }
        }

        protected virtual string GetViewName(IPage page)
        {
            string viewName;
            if (page.PageTemplate.MetadataFields == null || !page.PageTemplate.MetadataFields.ContainsKey("view")) // TODO: add to Constants.cs
            {
                viewName = page.PageTemplate.Title.Replace(" ", "");
            }
            else
            {
                viewName = page.PageTemplate.MetadataFields["view"].Value;
            }
            if (string.IsNullOrEmpty(viewName))
            {
                throw new ConfigurationException("no view configured for page template " + page.PageTemplate.Id);
            }
            return viewName;
        }

        protected virtual string GetViewName(IComponentPresentation componentPresentation)
        {
            string viewName = null;
            if (componentPresentation.ComponentTemplate.MetadataFields == null || !componentPresentation.ComponentTemplate.MetadataFields.ContainsKey("view"))
                viewName = componentPresentation.ComponentTemplate.Title.Replace(" ", "");
            else
                viewName = componentPresentation.ComponentTemplate.MetadataFields["view"].Value;

            if (string.IsNullOrEmpty(viewName))
            {
                throw new ConfigurationException("no view configured for component template " + componentPresentation.ComponentTemplate.Id);
            }
            return viewName;
        }

        protected string AddWelcomePageToUrl(string url)
        {

            if (string.IsNullOrEmpty(url))
            {
                url = WelcomeFileName;
            }
            else
            {
                if (reDefaultPage.IsMatch("/" + url))
                {
                    if (url.EndsWith("/"))
                    {
                        url += WelcomeFileName;
                    }
                    else
                    {
                        url += "/" + WelcomeFileName;
                    }
                }
            }

            url = "/" + url;
            return url;
        }


    }
}
