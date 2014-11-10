using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Mvc.Controllers;
using System;
using System.Collections.Specialized;
using System.Reflection;
using System.Web;

namespace Sdl.Web.Modules.Search
{
    public class SearchController : BaseController
    {
        public virtual ISearchProvider SearchProvider { get; set; }
        public SearchController(IContentProvider contentProvider, IRenderer renderer, ISearchProvider searchProvider)
        {
            ContentProvider = contentProvider;
            SearchProvider = searchProvider;
            SearchProvider.ContentResolver = contentProvider.ContentResolver;
            Renderer = renderer;
        }

        override protected object ProcessModel(object sourceModel, Type type)
        {
            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(SearchQuery<>)))
            {
                var loc = WebRequestContext.Localization;
                var searchIndex = SiteConfiguration.GetConfig("search." + (loc.IsStaging ? "staging" : "live") + "IndexConfig");
                //Use reflection to execute the generic method ISearchProvider.ExecuteQuery
                //As we do not know the generic type until runtime (its specified by the view model)
                Type resultType = type.GetGenericArguments()[0];
                MethodInfo method = typeof(ISearchProvider).GetMethod("ExecuteQuery");
                MethodInfo generic = method.MakeGenericMethod(resultType);
                NameValueCollection parameters = Request.QueryString;
                return generic.Invoke(SearchProvider, new object[] { parameters, sourceModel, searchIndex });
            }
            else
            {
                Exception ex = new Exception("Cannot run query - View Model is not of type SearchQuery<T>.");
                Log.Error(ex);
                throw ex;
            }
        }
    }
}
