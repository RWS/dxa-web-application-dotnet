using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Sdl.Web.Mvc.Models;
using Sdl.Web.Mvc.Html;
using Sdl.Web.Mvc.Mapping;
using System.Web;

namespace Sdl.Web.Mvc
{
    public abstract class BaseController : Controller
    {
        public virtual IContentProvider ContentProvider { get; set; }
        public virtual IRenderer Renderer { get; set; }
        
        //These will be implemented by content provider specific implementations of the BaseController
        protected abstract object GetModelForPage(string pageUrl);
        protected abstract string GetContentForPage(string pageUrl);

        [HandleError]
        public ActionResult Page(string pageUrl)
        {
            //We can have a couple of tries to get the page model if there is no file extension on the url request, but it does not end in a slash:
            //1. Try adding the default extension, so /news becomes /news.html
            var model = this.GetModelForPage(ParseUrl(pageUrl));
            if (model == null && !pageUrl.EndsWith("/") && pageUrl.LastIndexOf(".") <= pageUrl.LastIndexOf("/"))
            {
                //2. Try adding the default page, so /news becomes /news/index.html
                model = this.GetModelForPage(ParseUrl(pageUrl + "/"));
            }
            if (model == null)
            {
                throw new HttpException(404, "Page cannot be found");
            }
            ViewBag.Renderer = Renderer;
            //TODO - ViewBag.InlineEditingBootstrap = Markup.GetInlineEditingBootstrap(model);
            return GetPageView(model);
        }

        [HandleSectionError(View = "_SectionError")]
        public virtual ActionResult Region(Region region)
        {
            ViewBag.Renderer = Renderer;
            return GetRegionView(region);
        }

        [MapModel]
        [HandleSectionError(View = "_SectionError")]
        public virtual ActionResult Entity(object entity)
        {
            ViewBag.Renderer = Renderer;
            return GetEntityView(entity);
        }

        public virtual string ParseUrl(string url)
        {
            var defaultPageFileName = Configuration.GetDefaultPageName();
            return string.IsNullOrEmpty(url) ? defaultPageFileName : (url.EndsWith("/") ? url + defaultPageFileName : url += Configuration.GetDefaultExtension());
        }

        protected virtual ViewResult GetPageView(object page)
        {
            var viewName = ContentProvider.GetPageViewName(page);
            var subPages = new Dictionary<string, object>();
            var headerModel = this.GetModelForPage(Configuration.LocalizeUrl(ParseUrl("system/include/header")));
            var footerModel = this.GetModelForPage(Configuration.LocalizeUrl(ParseUrl("system/include/footer")));
            
            
            if (headerModel != null)
            {
                subPages.Add("Header", headerModel);
            }
            if (footerModel != null)
            {
                subPages.Add("Footer", footerModel);
            }
            var model = ContentProvider.CreatePageModel(page, subPages, viewName);
            return base.View(viewName, model);
        }

        protected virtual ViewResult GetRegionView(Region region)
        {
            return base.View(GetRegionViewName(region), region);
        }

        protected virtual string GetRegionViewName(Region region)
        {
            var viewName = region.Name.Replace(" ", "");
            var module = Configuration.GetDefaultModuleName();
            return String.Format("{0}/{1}", module, viewName); 
        }

        protected virtual ViewResult GetEntityView(object entity)
        {
            return View(entity);
        }
    }
}
