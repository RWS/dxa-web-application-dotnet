using System.Web.Mvc;

namespace Sdl.Web.Mvc.Formats
{
    public class JsonViewResultFormatter : MediaTypeViewResultFormatter
    {
        public JsonViewResultFormatter()
        {
            AddSupportedMediaType("application/json");
        }

        public override ActionResult CreateResult(ControllerContext controllerContext, ActionResult currentResult)
        {
            var model = controllerContext.Controller.ViewData.Model;

            if (model == null)
                return null;

            return new JsonNetResult { Data = model };
        }
    }
}
