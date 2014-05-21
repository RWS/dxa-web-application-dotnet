using System.Collections.Generic;

namespace Sdl.Web.Mvc.Html
{
    public abstract class BaseRenderer : IRenderer
    {
        public abstract System.Web.Mvc.MvcHtmlString Render(object item, System.Web.Mvc.HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null);

        public abstract System.Web.Mvc.MvcHtmlString Render(Models.Region region, System.Web.Mvc.HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null);
    }
}
