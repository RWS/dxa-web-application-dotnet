using System.Web.Mvc;
using System.Web.Mvc.Html;
using DD4T.ContentModel;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Html;
using Sdl.Web.Mvc.Models;

namespace Sdl.Web.DD4T
{
    public class DD4TRenderer : BaseRenderer
    {
        public override MvcHtmlString Render(object item, HtmlHelper helper)
        {
            var cp = item as IComponentPresentation;
            if (cp!=null)
            {
                string controller = Configuration.GetEntityController();
                string action = Configuration.GetEntityAction();
                if (cp.ComponentTemplate.MetadataFields != null && cp.ComponentTemplate.MetadataFields.ContainsKey("controller"))
                {
                    controller = cp.ComponentTemplate.MetadataFields["controller"].Value;
                }
                if (cp.ComponentTemplate.MetadataFields != null && cp.ComponentTemplate.MetadataFields.ContainsKey("action"))
                {
                    action = cp.ComponentTemplate.MetadataFields["action"].Value;
                }
                MvcHtmlString result = helper.Action(action, controller, new { entity = cp });
                return Markup.Parse(result,cp);
            }
            return null;
        }

        public override MvcHtmlString Render(Region region, HtmlHelper helper)
        {
            if (region != null)
            {
                string controller = Configuration.GetRegionController();
                string action = Configuration.GetRegionAction();
                MvcHtmlString result = helper.Action(action, controller, new { Region = region });
                return Markup.Parse(result, region);
            }
            return null;
            
        }
    }
}
