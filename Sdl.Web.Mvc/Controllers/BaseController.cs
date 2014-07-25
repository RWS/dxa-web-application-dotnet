using System;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models.Common;
using Sdl.Web.Common.Models.Entity;
using Sdl.Web.Common.Models.Interfaces;
using Sdl.Web.Common.Models.Navigation;
using Sdl.Web.Common.Models.Page;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Mvc.ContentProvider;

namespace Sdl.Web.Mvc.Controllers
{
    public abstract class BaseController : Controller
    {
        public virtual IContentProvider ContentProvider { get; set; }
        public virtual IRenderer Renderer { get; set; }
        public virtual ModelType ModelType { get; set; }

        protected BaseController()
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
            var model = ProcessModel(page, GetViewType(viewData)) ?? page;
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
            var model = ProcessModel(region, GetViewType(viewData)) ?? region;
            return View(viewData.ViewName, model);
        }

        [HandleSectionError(View = "_SectionError")]
        public virtual ActionResult Entity(object entity, int containerSize = 0)
        {
            DateTime timerStart = DateTime.Now;
            ModelType = ModelType.Entity;
            var viewData = GetViewData(entity);
            SetupViewData(containerSize, viewData.AreaName);
            var model = ProcessModel(entity, GetViewType(viewData)) ?? entity;
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
            var model = ProcessList(entity, GetViewType(viewData)) ?? entity;
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
            var model = ProcessNavigation(entity, GetViewType(viewData), navType) ?? entity;
            Log.Trace(timerStart, "navigation-processed", viewData.ViewName);
            return View(viewData.ViewName, model);
        }

        [HandleError]
        public virtual ActionResult SiteMap()
        {
            var model = ContentProvider.GetNavigationModel(SiteConfiguration.LocalizeUrl("navigation.json", WebRequestContext.Localization));
            return View(model);
        }

        [HandleError]
        public ActionResult Resolve(string itemId, string localization)
        {
            //TODO remove this tcm specific code here
            var url = ContentProvider.ContentResolver.ResolveLink("tcm:" + itemId, localization);
            if (url == null)
            {
                var bits = itemId.Split(':');
                if (bits.Length > 1)
                {
                    bits = bits[1].Split('-');
                    foreach (var loc in SiteConfiguration.Localizations.Values)
                    {
                        if (loc.LocalizationId == bits[0])
                        {
                            url = loc.Path;
                        }
                    }
                }
            }
            if (url == null)
            {
                if (localization == null)
                {
                    url = SiteConfiguration.DefaultLocalization;
                }
                else
                {
                    //var loc = SiteConfiguration.Localizations.Values.Where(l => l.LocalizationId.ToString() == localization).FirstOrDefault();
                    var loc = SiteConfiguration.Localizations.Values.FirstOrDefault(l => l.LocalizationId.ToString(CultureInfo.InvariantCulture) == localization);
                    if (loc != null)
                    {
                        url = loc.Path;
                    }
                }
            }
            if (url != null)
            {
                Response.Redirect(url, true);
            }
            return null;
        }

        protected virtual void SetupViewData(int containerSize = 0, string areaName = null)
        {
            ViewBag.Renderer = Renderer;
            ViewBag.ContainerSize = containerSize;
            if (areaName != null)
            {
                //This enables us to jump areas when rendering sub-views - for example from rendering a region in Core to an entity in ModuleX
                ControllerContext.RouteData.DataTokens["area"] = areaName;
            }
        }

        protected virtual Type GetViewType(MvcData viewData)
        {
            var key = String.Format("{0}:{1}", viewData.AreaName, viewData.ViewName);
            if (!SiteConfiguration.ViewModelRegistry.ContainsKey(key))
            {
                //TODO - take into account area?
                var viewEngineResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewData.ViewName);
                if (viewEngineResult.View == null)
                {
                    Log.Error("Could not find view {0} in locations: {1}", viewData.ViewName, String.Join(",", viewEngineResult.SearchedLocations));
                    throw new Exception(String.Format("Missing view: {0}", viewData.ViewName));
                }

                //This is the only way to get the view model type from the view and thus prevent the need to configure this somewhere
                var path = ((BuildManagerCompiledView)viewEngineResult.View).ViewPath;
                SiteConfiguration.AddViewModelToRegistry(viewData, path);
            }
            return SiteConfiguration.ViewModelRegistry[key];
        }

        protected virtual MvcData GetViewData(object sourceModel)
        {
            return ContentProvider.ContentResolver.ResolveMvcData(sourceModel);
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
                catch (Exception)
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
                    ContentProvider.PopulateDynamicList(list);
                }
                model = list;
            }
            return model;
        }

        protected virtual object ProcessNavigation(object sourceModel, Type type, string navType)
        {
            var navigationUrl = SiteConfiguration.LocalizeUrl("navigation.json", WebRequestContext.Localization);
            var model = ProcessModel(sourceModel, type);
            var nav = model as NavigationLinks;
            NavigationLinks links = new NavigationLinks();
            switch (navType)
            {
                case "Top":
                    links = new NavigationBuilder { ContentProvider = ContentProvider, NavigationUrl = navigationUrl }.BuildTopNavigation(Request.Url.LocalPath);
                    break;
                case "Left":
                    links = new NavigationBuilder { ContentProvider = ContentProvider, NavigationUrl = navigationUrl }.BuildContextNavigation(Request.Url.LocalPath);
                    break;
                case "Breadcrumb":
                    links = new NavigationBuilder { ContentProvider = ContentProvider, NavigationUrl = navigationUrl }.BuildBreadcrumb(Request.Url.LocalPath);
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
