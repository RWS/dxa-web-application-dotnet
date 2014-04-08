using DD4T.ContentModel;
using DD4T.Utils;
using HtmlAgilityPack;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace Sdl.Web.DD4T
{
    public class DD4TRenderer : BaseRenderer
    {
        public override System.Web.Mvc.MvcHtmlString Render(object item, HtmlHelper helper)
        {
            var cp = item as IComponentPresentation;
            if (cp!=null)
            {
                string controller = ConfigurationHelper.ComponentPresentationController;
                string action  = ConfigurationHelper.ComponentPresentationAction;
                if (cp.ComponentTemplate.MetadataFields != null && cp.ComponentTemplate.MetadataFields.ContainsKey("controller"))
                {
                    controller = cp.ComponentTemplate.MetadataFields["controller"].Value;
                }
                if (cp.ComponentTemplate.MetadataFields != null && cp.ComponentTemplate.MetadataFields.ContainsKey("action"))
                {
                    action = cp.ComponentTemplate.MetadataFields["action"].Value;
                }
                MvcHtmlString result = helper.Action(action, controller, new { componentPresentation = cp });
                return Semantics.Parse(result,cp);
            }
            return null;
        }


    }
}
