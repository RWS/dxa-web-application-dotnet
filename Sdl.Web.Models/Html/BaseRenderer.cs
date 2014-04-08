using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sdl.Web.Mvc.Models;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace Sdl.Web.Mvc.Html
{
    public abstract class BaseRenderer : IRenderer
    {
        public abstract System.Web.Mvc.MvcHtmlString Render(object item, System.Web.Mvc.HtmlHelper helper);

        public System.Web.Mvc.MvcHtmlString Render(Models.Region region, System.Web.Mvc.HtmlHelper helper)
        {
            if (region != null)
            {
                string controller = Configuration.GetRegionController();
                string action = Configuration.GetRegionAction();
                MvcHtmlString result = helper.Action(action, controller, new { Region = region });
                return result;
            }
            return null;
        }

    }
}
