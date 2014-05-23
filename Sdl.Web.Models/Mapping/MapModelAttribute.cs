using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Sdl.Web.Mvc.Mapping
{
    /// <summary>
    /// Custom Action Filter Attribute to enable controller actions to provide Domain model -> View model mapping via a Content Provider
    /// </summary>
    public class MapModelAttribute : ActionFilterAttribute
    {
        public IContentProvider ContentProvider { get; set; }
        public ModelType ModelType { get; set; }
        public string[] Includes { get; set; }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var viewResult = (ViewResult)filterContext.Result;
            var sourceModel = filterContext.Controller.ViewData.Model;
            if (ContentProvider == null)
            {
                ContentProvider = ((BaseController)filterContext.Controller).ContentProvider;
            }
            var viewName = String.IsNullOrEmpty(viewResult.ViewName) ? GetViewName(sourceModel) : viewResult.ViewName;
            var viewEngineResult = ViewEngines.Engines.FindPartialView(filterContext.Controller.ControllerContext, viewName);
            if (viewEngineResult.View == null)
            {
                Log.Error("Could not find view {0} in locations: {1}",viewName, String.Join(",", viewEngineResult.SearchedLocations));
                throw new Exception(String.Format("Missing view: {0}",viewName));
            }
            else
            {
                if (!Configuration.ViewModelRegistry.ContainsKey(viewName))
                {
                    //This is the only way to get the view model type from the view and thus prevent the need to configure this somewhere
                    var path = ((BuildManagerCompiledView)viewEngineResult.View).ViewPath;
                    Configuration.AddViewModelToRegistry(viewName, path);
                }
                //If the content provider does not return a view model, then we use the source model
                var model = MapModel(sourceModel, Configuration.ViewModelRegistry[viewName]) ?? sourceModel;
                filterContext.Controller.ViewData.Model = model;
                viewResult.ViewName = viewName;
            }
        }

        public string GetViewName(object sourceModel)
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
        public object MapModel(object sourceModel, Type type)
        {
            List<object> includes = GetIncludes();
            return ContentProvider.MapModel(sourceModel, ModelType, type, includes);
        }

        private List<object> GetIncludes()
        {
            if (Includes != null && Includes.Length > 0)
            {
                List<object> result = new List<object>();
                foreach (var includeUrl in Includes)
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

        private object GetInclude(string includeUrl)
        {
            switch (ModelType)
            {
                case ModelType.Page:
                    return ContentProvider.GetPageModel(includeUrl);
                case ModelType.Region:
                    return null;
                default:
                    return ContentProvider.GetEntityModel(includeUrl);
            }
        }
    }

    public enum ModelType{
        Page,
        Region,
        Entity
    }
}
