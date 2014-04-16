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
    public class DD4TController : Controller
    {
        public virtual Fac.IPageFactory PageFactory { get; set; }
        public virtual Fac.IComponentFactory ComponentFactory { get; set; }
        public virtual IModelFactory ModelFactory { get; set; }
        public virtual IRenderer Renderer { get; set; }

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
            this.ComponentFactory = new ComponentFactory()
            {
                ComponentProvider = new TridionComponentProvider(),
                PublicationResolver = new PublicationResolver()                
            };
        }


        [HandleError]
        public ActionResult Page(string pageId)
        {
            //We can have a couple of tries to get the page model if there is no file extension on the url request, but it does not end in a slash:
            //1. Try adding the default extension, so /news becomes /news.html
            var model = this.GetModelForPage(ParseUrl(pageId));
            if (model==null && !pageId.EndsWith("/") && pageId.LastIndexOf(".") <= pageId.LastIndexOf("/"))
            {
                //2. Try adding the default page, so /news becomes /news/index.html
                model = this.GetModelForPage(ParseUrl(pageId + "/"));
            }
            if (model == null)
            {
                throw new HttpException(404, "Page cannot be found");
            }
            ViewBag.Renderer = Renderer;
            ViewBag.InlineEditingBootstrap = Markup.GetInlineEditingBootstrap(model);
            return GetView(model);
        }

        public virtual ActionResult Region(Region region)
        {
            ViewBag.Renderer = Renderer;
            return GetView(region);
        }

        public virtual ActionResult ComponentPresentation(IComponentPresentation componentPresentation)
        {
            ViewBag.Renderer = Renderer;
            return GetView(componentPresentation);
        }

        protected IPage GetModelForPage(string PageId)
        {
            IPage page;
            if (PageFactory != null)
            {
                if (PageFactory.TryFindPage(string.Format("/{0}", PageId), out page))
                {
                    return page;
                }
            }
            else
                throw new ConfigurationException("No PageFactory configured");

            return page;
        }
        
        protected ViewResult GetView(IPage page)
        {
            string viewName = GetViewName(page);
            var model = ModelFactory.CreatePageModel(page, viewName);
            return base.View(viewName, model);
        }

        protected virtual string GetViewName(IPage page)
        {
            var viewName = page.PageTemplate.Title.Replace(" ", "");
            if (page.PageTemplate.MetadataFields != null && page.PageTemplate.MetadataFields.ContainsKey("view"))
            {
                viewName = page.PageTemplate.MetadataFields["view"].Value;
            }
            return viewName;
        }

        public virtual string ParseUrl(string url)
        {
            var defaultPageFileName = Configuration.GetDefaultPageName();
            return string.IsNullOrEmpty(url) ? defaultPageFileName : (url.EndsWith("/") ? url + defaultPageFileName : url += Configuration.GetDefaultExtension());
        }
        protected virtual ViewResult GetView(Region region)
        {
            return base.View(GetViewName(region), region);
        }

        protected virtual string GetViewName(Region region)
        {
            return region.Name.Replace(" ", "");
        }

        protected  ViewResult GetView(IComponentPresentation componentPresentation)
        {
            var viewName = GetViewName(componentPresentation);
            return View(viewName, ModelFactory.CreateEntityModel(componentPresentation, viewName));
        }

        protected virtual string GetViewName(IComponentPresentation componentPresentation)
        { 
            var template = componentPresentation.ComponentTemplate;
            //strip region and whitespace
            string viewName = Regex.Replace(template.Title, @"\[.*\]|\s", "");
            if (template.MetadataFields != null && template.MetadataFields.ContainsKey("view"))
            {
                viewName = componentPresentation.ComponentTemplate.MetadataFields["view"].Value;
            }
            return viewName;
        }
    }
}
