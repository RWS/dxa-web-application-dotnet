using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Interfaces
{
    // TODO TSI-788: [Obsolete("Deprecated in DXA 1.1. Use DXA HtmlHelper extension methods instead.")]
#pragma warning disable 618
    public interface IRenderer
    {
        MvcHtmlString RenderEntity(object item, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null);
        MvcHtmlString RenderRegion(IRegion region, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null);
        MvcHtmlString RenderPageData(IPage page, HtmlHelper helper);
        MvcHtmlString RenderIncludePageData(IPage page, HtmlHelper helper);
    }
#pragma warning restore 618
}