using System.Web.Mvc;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Mvc.Controllers
{
    public class EntityController : BaseController
    {
        /// <summary>
        /// Map and render an entity model
        /// </summary>
        /// <param name="entity">The entity model</param>
        /// <param name="containerSize">The size (in grid units) of the container the entity is in</param>
        /// <returns>Rendered entity model</returns>
        [HandleSectionError(View = "SectionError")]
        public virtual ActionResult Entity(EntityModel entity, int containerSize = 0)
        {
            SetupViewData(entity, containerSize);
            EntityModel model = (EnrichModel(entity) as EntityModel) ?? entity;
            return View(model.MvcData.ViewName, model);
        }


    }
}
