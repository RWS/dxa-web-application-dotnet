using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using Sdl.Web.Mvc.Html;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.Mvc.Models;

namespace Sdl.Web.Mvc
{
    public abstract class BaseController : Controller
    {
        public virtual IContentProvider ContentProvider { get; set; }
        public virtual IRenderer Renderer { get; set; }

        [MapModel(ModelType=ModelType.Page, Includes= new string[]{"system/include/header","system/include/footer"})]
        [HandleError]
        public virtual ActionResult Page(string pageUrl)
        {
            var model = ContentProvider.GetPageModel(pageUrl);
            if (model == null)
            {
                throw new HttpException(404, "Page cannot be found");
            }
            ViewBag.Renderer = Renderer;
            return GetPageView(model);
        }

        [MapModel(ModelType = ModelType.Region)]
        [HandleSectionError(View = "_SectionError")]
        public virtual ActionResult Region(Region region, int containerSize = 0)
        {
            ViewBag.Renderer = Renderer;
            ViewBag.ContainerSize = containerSize;
            return GetRegionView(region);
        }

        [MapModel(ModelType = ModelType.Entity)]
        [HandleSectionError(View = "_SectionError")]
        public virtual ActionResult Entity(object entity, int containerSize = 0)
        {
            ViewBag.Renderer = Renderer;
            ViewBag.ContainerSize = containerSize;
            return GetEntityView(entity);
        }

        protected virtual ViewResult GetPageView(object page)
        {
            var viewName = ContentProvider.GetPageViewName(page);
            return View(viewName, page);
        }

        protected virtual ViewResult GetRegionView(Region region)
        {
            var viewName = ContentProvider.GetRegionViewName(region);
            return View(viewName, region);
        }

        protected virtual ViewResult GetEntityView(object entity)
        {
            return View(entity);
        }
    }
}
