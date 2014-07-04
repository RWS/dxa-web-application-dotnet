using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using DD4T.ContentModel;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Html;
using Sdl.Web.Models;
using System;
using System.Web.Routing;
using interfaces = Sdl.Web.Models.Interfaces;
using Sdl.Web.Tridion;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;

namespace Sdl.Web.DD4T
{
    public class DD4TRenderer : BaseRenderer
    {
        public override MvcHtmlString RenderEntity(object item, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null)
        {
            var cp = item as IComponentPresentation;
            var mvcData = ContentResolver.ResolveMvcData(cp);
            if (cp != null && (excludedItems == null || !excludedItems.Contains(mvcData.ViewName)))
            {
                DateTime timerStart = DateTime.Now;
                var parameters = new RouteValueDictionary();
                int parentContainerSize = helper.ViewBag.ContainerSize;
                if (parentContainerSize == 0)
                {
                    parentContainerSize = Configuration.MediaHelper.GridSize;
                }
                if (containerSize == 0)
                {
                    containerSize = Configuration.MediaHelper.GridSize;
                }
                parameters["containerSize"] = (containerSize * parentContainerSize) / Configuration.MediaHelper.GridSize;
                parameters["entity"] = cp;
                parameters["area"] = mvcData.ControllerAreaName;
                foreach (var key in mvcData.RouteValues.Keys)
                {
                    parameters[key] = mvcData.RouteValues[key];
                }
                MvcHtmlString result = helper.Action(mvcData.ActionName, mvcData.ControllerName, parameters);
                Log.Trace(timerStart, "entity-render", cp.Component.Title);
                timerStart = DateTime.Now;
                if (WebRequestContext.IsPreview)
                {
                    result = new MvcHtmlString(TridionMarkup.ParseEntity(result.ToString()));
                }
                Log.Trace(timerStart, "entity-parse", cp.Component.Title);
                return result;
            }
            return null;
        }

        public override MvcHtmlString RenderRegion(interfaces.IRegion region, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null)
        {
            var mvcData = ContentResolver.ResolveMvcData(region);
            if (region != null && (excludedItems == null || !excludedItems.Contains(region.Name)))
            {
                DateTime timerStart = DateTime.Now;
                if (containerSize == 0)
                {
                    containerSize = Configuration.MediaHelper.GridSize;
                }
                MvcHtmlString result = helper.Action(mvcData.ActionName, mvcData.ControllerName, new { Region = region, containerSize = containerSize, area = mvcData.ControllerAreaName });
                Log.Trace(timerStart, "region-render", region.Name);
                timerStart = DateTime.Now;

                if (WebRequestContext.IsPreview)
                {
                    result = new MvcHtmlString(TridionMarkup.ParseRegion(result.ToString()));
                }
                Log.Trace(timerStart, "region-parse", region.Name);
                return result;
            }
            return null;
        }

        public override MvcHtmlString RenderPageData(interfaces.IPage page, HtmlHelper helper)
        {
            if (WebRequestContext.IsPreview)
            {
                if (!page.PageData.ContainsKey("CmsUrl"))
                {
                    page.PageData.Add("CmsUrl", Configuration.GetConfig("core.cmsurl"));
                }
                return new MvcHtmlString(TridionMarkup.PageMarkup(page.PageData));
            }
            return null;
        }
        
    }
}
