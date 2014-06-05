using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using Sdl.Web.Mvc.Html;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.Mvc.Models;
using Sdl.Web.Mvc.Common;

namespace Sdl.Web.Mvc
{
    public abstract class BaseController : Controller
    {
        public virtual IContentProvider ContentProvider { get; set; }
        public virtual IRenderer Renderer { get; set; }
        public virtual ModelType ModelType { get; set; }

        public BaseController()
        {
            ModelType = ModelType.Entity;
        }

        [HandleError]
        public virtual ActionResult Page(string pageUrl)
        {
            ModelType = ModelType.Page;
            var page = ContentProvider.GetPageModel(pageUrl);
            if (page == null)
            {
                throw new HttpException(404, "Page cannot be found");
            }
            SetupViewBag();
            var viewName = GetViewName(page);
            var model = this.ProcessModel(page, GetViewType(viewName),new List<string>{"system/include/header","system/include/footer"} ) ?? page;
            return View(viewName, model);
        }

        [HandleSectionError(View = "_SectionError")]
        public virtual ActionResult Region(Region region, int containerSize = 0)
        {
            ModelType = ModelType.Region;
            SetupViewBag(containerSize);
            var viewName = GetViewName(region);
            var model = this.ProcessModel(region, GetViewType(viewName)) ?? region;
            return View(viewName, model);
        }

        [HandleSectionError(View = "_SectionError")]
        public virtual ActionResult Entity(object entity, int containerSize = 0)
        {
            ModelType = ModelType.Entity;
            SetupViewBag(containerSize);
            var viewName = GetViewName(entity);
            var model = this.ProcessModel(entity, GetViewType(viewName)) ?? entity;
            return View(viewName, model);
        }

        protected virtual void SetupViewBag(int containerSize = 0)
        {
            ViewBag.Renderer = Renderer;
            ViewBag.ContainerSize = containerSize;
        }

        protected virtual Type GetViewType(string viewName)
        {
            var viewEngineResult = ViewEngines.Engines.FindPartialView(this.ControllerContext, viewName);
            if (viewEngineResult.View == null)
            {
                Log.Error("Could not find view {0} in locations: {1}", viewName, String.Join(",", viewEngineResult.SearchedLocations));
                throw new Exception(String.Format("Missing view: {0}", viewName));
            }
            else
            {
                if (!Configuration.ViewModelRegistry.ContainsKey(viewName))
                {
                    //This is the only way to get the view model type from the view and thus prevent the need to configure this somewhere
                    var path = ((BuildManagerCompiledView)viewEngineResult.View).ViewPath;
                    Configuration.AddViewModelToRegistry(viewName, path);
                }
                return Configuration.ViewModelRegistry[viewName];
            }
        }

        protected virtual string GetViewName(object sourceModel)
        {
            switch (ModelType)
            {
                case ModelType.Page:
                    return ContentProvider.GetPageViewName(sourceModel);
                case ModelType.Region:
                    return ContentProvider.GetRegionViewName(sourceModel);
                default:
                    return ContentProvider.GetEntityViewName(sourceModel);
            }
        }

        //This is the method to override if you need to add custom model population logic, first calling the base class and then adding your own logic
        protected virtual object ProcessModel(object sourceModel, Type type, List<string> includes = null)
        {
            List<object> includedItems = GetIncludes(includes);
            return ContentProvider.MapModel(sourceModel, ModelType, type, includedItems);
        }

        protected virtual List<object> GetIncludes(List<string> includes)
        {
            if (includes != null && includes.Count > 0)
            {
                List<object> result = new List<object>();
                foreach (var includeUrl in includes)
                {
                    object include = GetInclude(includeUrl);
                    if (include != null)
                    {
                        result.Add(include);
                    }
                }
                return result;
            }
            return null;
        }

        protected virtual object GetInclude(string includeUrl)
        {
            switch (ModelType)
            {
                case ModelType.Page:
                    return ContentProvider.GetPageModel(Configuration.LocalizeUrl(includeUrl));
                case ModelType.Region:
                    return null;
                default:
                    return ContentProvider.GetEntityModel(includeUrl);
            }
        }

    }
}
