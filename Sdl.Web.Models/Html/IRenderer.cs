using Sdl.Web.Mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Sdl.Web.Mvc.Html
{
    public interface IRenderer
    {
        MvcHtmlString Render(object item, HtmlHelper helper);
        MvcHtmlString Render(Region region, HtmlHelper helper);
    }
}
