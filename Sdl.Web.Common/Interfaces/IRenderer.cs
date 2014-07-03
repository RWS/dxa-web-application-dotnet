using Sdl.Web.Models.Interfaces;
using Sdl.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Sdl.Web.Common.Interfaces
{
    public interface IRenderer
    {
        MvcHtmlString RenderEntity(object item, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null);
        MvcHtmlString RenderRegion(IRegion region, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null);
        MvcHtmlString RenderPageData(IPage page, HtmlHelper helper);
    }
}
