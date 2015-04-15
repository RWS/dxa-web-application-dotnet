using System.Collections.Generic;
using System.Web.Mvc;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Interfaces
{
    public interface IRenderer
    {
        MvcHtmlString RenderEntity(object item, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null);
        MvcHtmlString RenderRegion(IRegion region, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null);
        MvcHtmlString RenderPageData(IPage page, HtmlHelper helper);
    }
}