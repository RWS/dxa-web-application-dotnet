using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using DD4T.ContentModel;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Context;
using Sdl.Web.Mvc.Html;
using Sdl.Web.Models;
using System;
using System.Web.Routing;
using interfaces = Sdl.Web.Models.Interfaces;

namespace Sdl.Web.DD4T
{
    public class DD4TRenderer : BaseRenderer
    {
        public override MvcHtmlString RenderEntity(object item, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null)
        {
            var cp = item as IComponentPresentation;
            if (cp != null && (excludedItems == null || !excludedItems.Contains(cp.ComponentTemplate.Title)))
            {
                DateTime timerStart = DateTime.Now;
                string controller = Configuration.GetEntityController();
                string action = Configuration.GetEntityAction();
                string area = Configuration.GetDefaultModuleName();
                var parameters = new RouteValueDictionary();
                int parentContainerSize = helper.ViewBag.ContainerSize;
                if (parentContainerSize == 0)
                {
                    parentContainerSize = ContextConfiguration.GridSize;
                }
                if (containerSize == 0)
                {
                    containerSize = ContextConfiguration.GridSize;
                }
                parameters["containerSize"] = (containerSize * parentContainerSize) / ContextConfiguration.GridSize;
                parameters["entity"] = cp;
                if (cp.ComponentTemplate.MetadataFields != null)
                {
                    if (cp.ComponentTemplate.MetadataFields.ContainsKey("controller"))
                    {
                        var bits = cp.ComponentTemplate.MetadataFields["controller"].Value.Split(':');
                        if (bits.Length > 1)
                        {
                            controller = bits[1];
                            area = bits[0];
                        }
                        else
                        {
                            controller = bits[0];
                        }
                    }
                    if (cp.ComponentTemplate.MetadataFields.ContainsKey("action"))
                    {
                        action = cp.ComponentTemplate.MetadataFields["action"].Value;
                    }
                    if (cp.ComponentTemplate.MetadataFields.ContainsKey("routeValues"))
                    {
                        var bits = cp.ComponentTemplate.MetadataFields["routeValues"].Value.Split(',');
                        foreach (string bit in bits)
                        {
                            var parameter = bit.Trim().Split(':');
                            if (parameter.Length > 1)
                            {
                                parameters[parameter[0]] = parameter[1];
                            }
                        }
                    }
                }
                parameters["area"] = area;
                MvcHtmlString result = helper.Action(action, controller, parameters);
                Log.Trace(timerStart, "entity-render", cp.Component.Title);
                timerStart = DateTime.Now;
                var res = Markup.ParseComponentPresentation(result);
                Log.Trace(timerStart, "entity-parse", cp.Component.Title);
                return res;
            }
            return null;
        }

        public override MvcHtmlString RenderRegion(interfaces.IRegion region, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null)
        {
            if (region != null && (excludedItems == null || !excludedItems.Contains(region.Name)))
            {
                DateTime timerStart = DateTime.Now;
                string controller = Configuration.GetRegionController();
                string action = Configuration.GetRegionAction();
                string area = region.Module;
                if (containerSize == 0)
                {
                    containerSize = ContextConfiguration.GridSize;
                }
                MvcHtmlString result = helper.Action(action, controller, new {Region = region, containerSize = containerSize, area=area });
                Log.Trace(timerStart, "region-render", region.Name);
                timerStart = DateTime.Now;
                var res = Markup.ParseRegion(result);
                Log.Trace(timerStart, "region-parse", region.Name);
                return res;
            }
            return null;

        }
    }
}
