using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Sdl.Web.Mvc.Html
{
    public abstract class BaseRenderer : IRenderer
    {
        public IContentResolver ContentResolver { get; set; }

        protected BaseRenderer()
        { 
        }

        protected BaseRenderer(IContentResolver resolver)
        {
            ContentResolver = resolver;
        }

        public abstract MvcHtmlString RenderEntity(object item, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null);
        public abstract MvcHtmlString RenderRegion(IRegion region, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null);
        public abstract MvcHtmlString RenderPageData(IPage page, HtmlHelper helper);
        public abstract MvcHtmlString RenderIncludePageData(IPage page, HtmlHelper helper);
    }
}
