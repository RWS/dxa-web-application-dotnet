using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Controllers;

namespace Sdl.Web.Modules.Search
{
    public class SearchController : BaseController
    {
        public virtual ISearchProvider SearchProvider { get; set; }
        public SearchController(IContentProvider contentProvider, IRenderer renderer, ISearchProvider searchProvider)
        {
            ContentProvider = contentProvider;
            SearchProvider = searchProvider;
            Renderer = renderer;
        }

        override protected object ProcessModel(object sourceModel, System.Type type)
        {
            var model = base.ProcessModel(sourceModel, type);
            var results = model as SearchResults<Teaser>;
            if (results != null) 
            {
                results.Query = new QueryData();
                results.Query.QueryText = Request.Params["q"];
                model = SearchProvider.ExecuteQuery(results);
            }
            return model;
        }
    }
}
