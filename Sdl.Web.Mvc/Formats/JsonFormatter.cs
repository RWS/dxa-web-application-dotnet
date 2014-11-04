using System.Web.Mvc;

namespace Sdl.Web.Mvc.Formats
{
    public class JsonFormatter : BaseFormatter
    {
        public JsonFormatter()
        {
            AddMediaType("application/json");
            this.ProcessModel = true;
            this.AddIncludes = false;
        }

        public override ActionResult FormatData(ControllerContext controllerContext, object model)
        {
            return model==null ? null : new JsonNetResult { Data = model };
        }

    }
}
