using DD4T.ContentModel;
using DD4T.Mvc.Controllers;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.Mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Net;
using Sdl.Web.Mvc.Html;
using DD4T.Providers.SDLTridion2013;
using DD4T.Factories;
using DD4T.Utils;
using Fac = DD4T.ContentModel.Factories;
using DD4T.ContentModel.Exceptions;
using System.Text.RegularExpressions;

namespace Sdl.Web.DD4T
{
    /// <summary>
    /// Port of TridionControllerBase, to add minor customizations (like removing the component presentation renderer) and 
    /// avoid having direct reference to DD4T.MVC in web app
    /// </summary>
    public class DD4TController : BaseController
    {
        public virtual Fac.IPageFactory PageFactory { get; set; }
        
        public DD4TController()
        {
            //TODO dependency injection?
            this.PageFactory = new PageFactory()
            {
                PageProvider = new TridionPageProvider(),
                PublicationResolver = new PublicationResolver(),
                ComponentFactory = new ComponentFactory() { PublicationResolver = new PublicationResolver() },
                LinkFactory = new LinkFactory() { PublicationResolver = new PublicationResolver() }
            };
        }

        protected override object GetModelForPage(string pageUrl)
        {
            IPage page;
            if (PageFactory != null)
            {
                if (PageFactory.TryFindPage(string.Format("/{0}", pageUrl), out page))
                {
                    ViewBag.InlineEditingBootstrap = Markup.GetInlineEditingBootstrap(page);
                    return page;
                }
            }
            else
                throw new ConfigurationException("No PageFactory configured");

            return page;
        }

   

        protected override string GetContentForPage(string pageUrl)
        {
            string page;
            if (PageFactory != null)
            {
                if (PageFactory.TryFindPageContent(string.Format("/{0}", pageUrl), out page))
                {
                    return page;
                }
            }
            else
                throw new ConfigurationException("No PageFactory configured");

            return page;
        }
        
        protected override string GetPageViewName(object pageObject)
        {
            var page = (IPage)pageObject;
            var viewName = page.PageTemplate.Title.Replace(" ", "");
            var module = Configuration.GetDefaultModuleName();
            if (page.PageTemplate.MetadataFields != null)
            {
                if (page.PageTemplate.MetadataFields.ContainsKey("view"))
                {
                    viewName = page.PageTemplate.MetadataFields["view"].Value;
                }
                if (page.PageTemplate.MetadataFields.ContainsKey("module"))
                {
                    module = page.PageTemplate.MetadataFields["module"].Value;
                }
            }
            return String.Format("{0}/{1}", module, viewName);
        }

        protected override string GetEntityViewName(object entity)
        {
            var componentPresentation = (ComponentPresentation)entity;
            var template = componentPresentation.ComponentTemplate;
            //strip region and whitespace
            string viewName = Regex.Replace(template.Title, @"\[.*\]|\s", "");
            var module = Configuration.GetDefaultModuleName();
            if (template.MetadataFields != null)
            {
                if (template.MetadataFields.ContainsKey("view"))
                {
                    viewName = componentPresentation.ComponentTemplate.MetadataFields["view"].Value;
                }
                if (template.MetadataFields.ContainsKey("module"))
                {
                    module = componentPresentation.ComponentTemplate.MetadataFields["module"].Value;
                }
            }
            return String.Format("{0}/{1}",module, viewName);
        }
    }
}
