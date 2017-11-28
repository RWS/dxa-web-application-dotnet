using System.Web.Mvc;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Mvc.Controllers
{
    public class RegionController : BaseController
    {
        /// <summary>
        /// Map and render a region model
        /// </summary>
        /// <param name="region">The region model</param>
        /// <param name="containerSize">The size (in grid units) of the container the region is in</param>
        /// <returns>Rendered region model</returns>
        [HandleSectionError(View = "SectionError")]
        public virtual ActionResult Region(RegionModel region, int containerSize = 0)
        {
            SetupViewData(region, containerSize);
            RegionModel model = (EnrichModel(region) as RegionModel) ?? region;
            return View(model.MvcData.ViewName, model);
        }
    }
}
