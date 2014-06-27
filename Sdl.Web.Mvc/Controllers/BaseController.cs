using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using Sdl.Web.Mvc.Html;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.Models;
using Sdl.Web.Common.Interfaces;
using System.Linq;
using Sdl.Web.Models.Interfaces;

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
            DateTime timerStart = DateTime.Now;
            ModelType = ModelType.Page;
            var page = ContentProvider.GetPageModel(pageUrl);
            if (page == null)
            {
                throw new HttpException(404, "Page cannot be found");
            }
            var viewData = GetViewData(page);
            SetupViewData(0,viewData.AreaName);
            var model = this.ProcessModel(page, GetViewType(viewData)) ?? page;
            if (model is WebPage)
            {
                WebRequestContext.PageId = ((WebPage)model).Id;
            }
            Log.Trace(timerStart, "page-mapped", pageUrl);
            return View(viewData.ViewName, model);
        }

        [HandleSectionError(View = "_SectionError")]
        public virtual ActionResult Region(IRegion region, int containerSize = 0)
        {
            ModelType = ModelType.Region;
            SetupViewData(containerSize, region.Module);
            var viewData = GetViewData(region);
            var model = this.ProcessModel(region, GetViewType(viewData)) ?? region;
            return View(viewData.ViewName, model);
        }

        [HandleSectionError(View = "_SectionError")]
        public virtual ActionResult Entity(object entity, int containerSize = 0)
        {
            DateTime timerStart = DateTime.Now;
            ModelType = ModelType.Entity;
            var viewData = GetViewData(entity);
            SetupViewData(containerSize, viewData.AreaName);
            var model = this.ProcessModel(entity, GetViewType(viewData)) ?? entity;
            Log.Trace(timerStart, "entity-mapped", viewData.ViewName);
            return View(viewData.ViewName, model);
        }

        [HandleSectionError(View = "_SectionError")]
        public ActionResult List(object entity, int containerSize = 0)
        {
            DateTime timerStart = DateTime.Now;
            ModelType = ModelType.Entity;
            var viewData = GetViewData(entity);
            SetupViewData(containerSize, viewData.AreaName);
            var model = this.ProcessList(entity, GetViewType(viewData)) ?? entity;
            Log.Trace(timerStart, "list-processed", viewData.ViewName);
            return View(viewData.ViewName, model);
        }

        [HandleSectionError(View = "_SectionError")]
        public virtual ActionResult Navigation(object entity, string navType, int containerSize = 0)
        {
            DateTime timerStart = DateTime.Now;
            ModelType = ModelType.Entity;
            var viewData = GetViewData(entity);
            SetupViewData(containerSize, viewData.AreaName);
            var model = this.ProcessNavigation(entity, GetViewType(viewData), navType) ?? entity;
            Log.Trace(timerStart, "navigation-processed", viewData.ViewName);
            return View(viewData.ViewName, model);
        }

        [HandleError]
        public virtual ActionResult SiteMap()
        {
            var model = ContentProvider.GetNavigationModel(Configuration.LocalizeUrl("navigation.json"));
            return View(model);
        }

        [HandleError]
        public ActionResult Resolve(string itemId, string localization)
        {
            //TODO remove this tcm specific code here
            var url = ContentProvider.ProcessUrl("tcm:" + itemId, localization);
            if (url == null)
            {
                var bits = itemId.Split(':');
                if (bits.Length > 1)
                {
                    bits = bits[1].Split('-');
                    int pubid = 0;
                    if (Int32.TryParse(bits[0], out pubid))
                    {
                        foreach (var loc in Configuration.Localizations.Values)
                        {
                            if (loc.LocalizationId == pubid)
                            {
                                url = loc.Path;
                            }
                        }
                    }
                }
            }
            if (url == null)
            {
                if (localization == null)
                {
                    url = Configuration.DefaultLocalization;
                }
                else
                {
                    var loc = Configuration.Localizations.Values.Where(l => l.LocalizationId.ToString() == localization).FirstOrDefault();
                    if (loc != null)
                    {
                        url = loc.Path;
                    }
                }
            }
            Response.Redirect(url, true);
            return null;
        }

        protected virtual void SetupViewData(int containerSize = 0, string areaName = null)
        {
            ViewBag.Renderer = Renderer;
            ViewBag.ContainerSize = containerSize;
            if (areaName != null)
            {
                //This enables us to jump areas when rendering sub-views - for example from rendering a region in Core to an entity in ModuleX
                this.ControllerContext.RouteData.DataTokens["area"] = areaName;
            }
        }

        protected virtual Type GetViewType(ViewData viewData)
        {
            var key = String.Format("{0}:{1}", viewData.AreaName, viewData.ViewName);
            if (!Configuration.ViewModelRegistry.ContainsKey(key))
            {
                //TODO - take into account area?
                var viewEngineResult = ViewEngines.Engines.FindPartialView(this.ControllerContext, viewData.ViewName);
                if (viewEngineResult.View == null)
                {
                    Log.Error("Could not find view {0} in locations: {1}", viewData.ViewName, String.Join(",", viewEngineResult.SearchedLocations));
                    throw new Exception(String.Format("Missing view: {0}", viewData.ViewName));
                }
                else
                {
                    //This is the only way to get the view model type from the view and thus prevent the need to configure this somewhere
                    var path = ((BuildManagerCompiledView)viewEngineResult.View).ViewPath;
                    Configuration.AddViewModelToRegistry(viewData, path);
                }
            }
            return Configuration.ViewModelRegistry[key];
        }

        protected virtual ViewData GetViewData(object sourceModel)
        {
            switch (ModelType)
            {
                case ModelType.Page:
                    return ContentProvider.GetPageViewData(sourceModel);
                case ModelType.Region:
                    return ContentProvider.GetRegionViewData(sourceModel);
                default:
                    return ContentProvider.GetEntityViewData(sourceModel);
            }
        }

        //This is the method to override if you need to add custom model population logic, first calling the base class and then adding your own logic
        protected virtual object ProcessModel(object sourceModel, Type type)
        {
            return ContentProvider.MapModel(sourceModel, ModelType, type);
        }

        private T GetRequestParameter<T>(string name)
        {
            var val = Request.Params[name];
            if (!String.IsNullOrEmpty(val))
            {
                try
                {
                    return (T)Convert.ChangeType(val,typeof(T));
                }
                catch (Exception ex)
                {
                    Log.Warn("Could not convert request parameter {0} into type {1}, using type default.", val, typeof(T));
                }
            }
            return default(T);
        }


        protected virtual object ProcessList(object sourceModel, Type type)
        {
            var model = ProcessModel(sourceModel, type);
            var list = model as ContentList<Teaser>;
            if (list != null)
            {
                if (list.ItemListElements.Count == 0)
                {
                    //we need to run a query to populate the list
                    int start = GetRequestParameter<int>("start");
                    if (list.Id == Request.Params["id"])
                    {
                        //we only take the start from the query string if there is also an id parameter matching the model entity id
                        //this means that we are sure that the paging is coming from the right entity (if there is more than one paged list on the page)
                        list.CurrentPage = (start / list.PageSize) + 1;
                        list.Start = start;
                    }
                    this.ContentProvider.PopulateDynamicList(list);
                }
                model = list;
            }
            return model;
        }


        protected virtual object ProcessNavigation(object sourceModel, Type type, string navType)
        {
            var navigationUrl = Configuration.LocalizeUrl("navigation.json");
            var model = ProcessModel(sourceModel, type);
            var nav = model as NavigationLinks;
            NavigationLinks links = new NavigationLinks();
            switch (navType)
            {
                case "Top":
                    links = new NavigationBuilder() { ContentProvider = this.ContentProvider, NavigationUrl = navigationUrl }.BuildTopNavigation(Request.Url.LocalPath.ToString());
                    break;
                case "Left":
                    links = new NavigationBuilder() { ContentProvider = this.ContentProvider, NavigationUrl = navigationUrl }.BuildContextNavigation(Request.Url.LocalPath.ToString());
                    break;
                case "Breadcrumb":
                    links = new NavigationBuilder() { ContentProvider = this.ContentProvider, NavigationUrl = navigationUrl }.BuildBreadcrumb(Request.Url.LocalPath.ToString());
                    break;
            }
            if (nav != null)
            {

                links.EntityData = nav.EntityData;
                links.PropertyData = nav.PropertyData;
            }
            return links;
        }

    }
}
