using System.Linq;
using System.Web.Mvc;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Mvc.OutputCache;

namespace Sdl.Web.Mvc.Controllers
{
    public class ListController : EntityController
    {
        /// <summary>
        /// Populate/Map and render a list entity model
        /// </summary>
        /// <param name="entity">The list entity model</param>
        /// <param name="containerSize">The size (in grid units) of the container the entity is in</param>
        /// <returns>Rendered list entity model</returns>
        [HandleSectionError(View = "SectionError")]
        [DxaOutputCache]
        public ActionResult List(EntityModel entity, int containerSize = 0)
        {
            // The List action is effectively just an alias for the general Entity action (we keep it for backward compatibility).
            return Entity(entity, containerSize);
        }

        protected override ViewModel EnrichModel(ViewModel sourceModel)
        {
            DynamicList model = base.EnrichModel(sourceModel) as DynamicList;
            if (model == null || model.QueryResults.Any())
            {
                return model;
            }

            //we need to run a query to populate the list
            if (model.Id == Request.Params["id"])
            {
                //we only take the start from the query string if there is also an id parameter matching the model entity id
                //this means that we are sure that the paging is coming from the right entity (if there is more than one paged list on the page)
                model.Start = GetRequestParameter<int>("start");
            }
            ContentProvider.PopulateDynamicList(model, WebRequestContext.Localization);

            return model;
        }
    }
}
