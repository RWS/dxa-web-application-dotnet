using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using DD4T.ContentModel;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Context;
using Sdl.Web.Mvc.Html;
using Sdl.Web.Mvc.Models;

namespace Sdl.Web.DD4T
{
    public class DD4TRenderer : BaseRenderer
    {
        public override MvcHtmlString Render(object item, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null)
        {
            var cp = item as IComponentPresentation;
            if (cp != null && (excludedItems == null || !excludedItems.Contains(cp.ComponentTemplate.Title)))
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
                int parentContainerSize = helper.ViewBag.ContainerSize;
                if (parentContainerSize == 0)
                {
                    parentContainerSize = ContextConfiguration.GridSize;
                }
                if (containerSize == 0)
                {
                    containerSize = ContextConfiguration.GridSize;
                }
                MvcHtmlString result = helper.Action(action, controller, new { entity = cp, containerSize = (containerSize * parentContainerSize) / ContextConfiguration.GridSize });
                return Markup.ParseComponentPresentation(result);
            }
            return null;
        }

        public override MvcHtmlString Render(Region region, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null)
        {
            if (region != null && (excludedItems == null || !excludedItems.Contains(region.Name)))
            {
                string controller = Configuration.GetRegionController();
                string action = Configuration.GetRegionAction();
                if (containerSize == 0)
                {
                    containerSize = ContextConfiguration.GridSize;
                }
                MvcHtmlString result = helper.Action(action, controller, new { Region = region, containerSize = containerSize });
                return Markup.ParseRegion(result);
            }
            return null;

        }
    }
}
