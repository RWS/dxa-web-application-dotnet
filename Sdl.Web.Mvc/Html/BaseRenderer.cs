using Sdl.Web.Models.Interfaces;
using Sdl.Web.Common.Interfaces;
using System.Collections.Generic;

namespace Sdl.Web.Mvc.Html
{
    public abstract class BaseRenderer : IRenderer
    {
        public IContentResolver ContentResolver { get; set; }
        public BaseRenderer()
        { 
        }
        public BaseRenderer(IContentResolver resolver)
        {
            ContentResolver = resolver;
        }
        public abstract System.Web.Mvc.MvcHtmlString RenderEntity(object item, System.Web.Mvc.HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null);
        public abstract System.Web.Mvc.MvcHtmlString RenderRegion(IRegion region, System.Web.Mvc.HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null);
        public abstract System.Web.Mvc.MvcHtmlString RenderPageData(IPage page, System.Web.Mvc.HtmlHelper helper);
    }
}
