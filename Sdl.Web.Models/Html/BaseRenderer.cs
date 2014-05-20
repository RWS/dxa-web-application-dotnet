using System.Web.Mvc;

namespace Sdl.Web.Mvc.Html
{
    public abstract class BaseRenderer : IRenderer
    {
        public abstract MvcHtmlString Render(object item, HtmlHelper helper);

        public abstract MvcHtmlString Render(Models.Region region, HtmlHelper helper);
    }
}
