using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
using DD4T.ContentModel;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Mvc.Html;
using Sdl.Web.Tridion.Markup;
using IPage = Sdl.Web.Common.Models.IPage;

namespace Sdl.Web.DD4T.Html
{
    /// <summary>
    /// Renderer implementation for DD4T
    /// </summary>
    public class DD4TRenderer : BaseRenderer
    {
        /// <summary>
        /// Render an entity (Component Presentation)
        /// </summary>
        /// <param name="item">The Component Presentation object</param>
        /// <param name="helper">The HTML Helper</param>
        /// <param name="containerSize">The size of the containing element (in grid units)</param>
        /// <param name="excludedItems">A list of view names, if the Component Presentation maps to one of these, it is skipped.</param>
        /// <returns>The rendered content</returns>
        public override MvcHtmlString RenderEntity(object item, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null)
        {
            var cp = item as IComponentPresentation;
            var mvcData = ContentResolver.ResolveMvcData(cp);
            if (cp != null && (excludedItems == null || !excludedItems.Contains(mvcData.ViewName)))
            {
                var parameters = new RouteValueDictionary();
                int parentContainerSize = helper.ViewBag.ContainerSize;
                if (parentContainerSize == 0)
                {
                    parentContainerSize = SiteConfiguration.MediaHelper.GridSize;
                }
                if (containerSize == 0)
                {
                    containerSize = SiteConfiguration.MediaHelper.GridSize;
                }
                parameters["containerSize"] = (containerSize * parentContainerSize) / SiteConfiguration.MediaHelper.GridSize;
                parameters["entity"] = cp;
                parameters["area"] = mvcData.ControllerAreaName;
                foreach (var key in mvcData.RouteValues.Keys)
                {
                    parameters[key] = mvcData.RouteValues[key];
                }
                MvcHtmlString result = helper.Action(mvcData.ActionName, mvcData.ControllerName, parameters);
                if (WebRequestContext.IsPreview)
                {
                    result = new MvcHtmlString(TridionMarkup.ParseEntity(result.ToString()));
                }
                return result;
            }
            return null;
        }

        /// <summary>
        /// Render an Region
        /// </summary>
        /// <param name="item">The Region object</param>
        /// <param name="helper">The HTML Helper</param>
        /// <param name="containerSize">The size of the containing element (in grid units)</param>
        /// <param name="excludedItems">A list of view names, if the Region maps to one of these, it is skipped.</param>
        /// <returns>The rendered content</returns>
        public override MvcHtmlString RenderRegion(IRegion region, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null)
        {
            var mvcData = ContentResolver.ResolveMvcData(region);
            if (region != null && (excludedItems == null || !excludedItems.Contains(region.Name)))
            {
                if (containerSize == 0)
                {
                    containerSize = SiteConfiguration.MediaHelper.GridSize;
                }
                MvcHtmlString result = helper.Action(mvcData.ActionName, mvcData.ControllerName, new { Region = region, containerSize = containerSize, area = mvcData.ControllerAreaName });
                
                if (WebRequestContext.IsPreview)
                {
                    result = new MvcHtmlString(TridionMarkup.ParseRegion(result.ToString()));
                }
                return result;
            }
            return null;
        }

        /// <summary>
        /// Render additional XPM page markup
        /// </summary>
        /// <param name="page">The DD4T Page object</param>
        /// <param name="helper">Html Helper</param>
        /// <returns>The page markup</returns>
        public override MvcHtmlString RenderPageData(IPage page, HtmlHelper helper)
        {
            if (WebRequestContext.IsPreview)
            {
                if (!page.PageData.ContainsKey("CmsUrl"))
                {
                    page.PageData.Add("CmsUrl", SiteConfiguration.GetConfig("core.cmsurl"));
                }
                return new MvcHtmlString(TridionMarkup.PageMarkup(page.PageData));
            }
            return null;
        }        
    }
}
