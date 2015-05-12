using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Mvc.Html;
using Sdl.Web.Tridion.Markup;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;

namespace Sdl.Web.DD4T.Html
{
    /// <summary>
    /// Renderer implementation for DD4T
    /// </summary>
    /// TODO TSI-788: This code is no longer DD4T-specific and should be moved to Sdl.Web.Mvc.
    public class DD4TRenderer : BaseRenderer
    {
        /// <summary>
        /// Render an Entity Model
        /// </summary>
        /// <param name="entity">The Entity Model</param>
        /// <param name="helper">The HTML Helper</param>
        /// <param name="containerSize">The size of the containing element (in grid units)</param>
        /// <param name="excludedItems">A list of view names, if the Component Presentation maps to one of these, it is skipped.</param>
        /// <returns>The rendered content</returns>
        public override MvcHtmlString RenderEntity(EntityModel entity, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null)
        {
            if (entity!=null)
            {
                MvcData mvcData = entity.MvcData;
                if (excludedItems == null || !excludedItems.Contains(mvcData.ViewName))
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
                    parameters["entity"] = entity;
                    parameters["area"] = mvcData.ControllerAreaName;
                    if (mvcData.RouteValues != null)
                    {
                        foreach (var key in mvcData.RouteValues.Keys)
                        {
                            parameters[key] = mvcData.RouteValues[key];
                        }
                    }
                    MvcHtmlString result = helper.Action(mvcData.ActionName, mvcData.ControllerName, parameters);
                    if (WebRequestContext.IsPreview)
                    {
                        // TODO: don't parse entity if this is in an include page (not rendered directly, so !WebRequestContext.IsInclude)
                        result = new MvcHtmlString(TridionMarkup.ParseEntity(result.ToString()));
                    }
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Render an Region
        /// </summary>
        /// <param name="region">The Region Model</param>
        /// <param name="helper">The HTML Helper</param>
        /// <param name="containerSize">The size of the containing element (in grid units)</param>
        /// <param name="excludedItems">A list of view names, if the Region maps to one of these, it is skipped.</param>
        /// <returns>The rendered content</returns>
        public override MvcHtmlString RenderRegion(RegionModel region, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null)
        {
            MvcData mvcData = region.MvcData;
            if (region != null && (excludedItems == null || !excludedItems.Contains(region.Name)))
            {
                if (containerSize == 0)
                {
                    containerSize = SiteConfiguration.MediaHelper.GridSize;
                }
                MvcHtmlString result = helper.Action(mvcData.ActionName, mvcData.ControllerName, new { Region = region, containerSize = containerSize, area = mvcData.ControllerAreaName });
                
                if (WebRequestContext.IsPreview)
                {
                    // TODO: don't parse region if this is a region in an include page (not rendered directly, so !WebRequestContext.IsInclude)
                    result = new MvcHtmlString(TridionMarkup.ParseRegion(result.ToString(),WebRequestContext.Localization));
                }
                return result;
            }
            return null;
        }

        /// <summary>
        /// Render additional XPM page markup
        /// </summary>
        /// <param name="page">The Page Model</param>
        /// <param name="helper">Html Helper</param>
        /// <returns>The page markup</returns>
        public override MvcHtmlString RenderPageData(PageModel page, HtmlHelper helper)
        {
            if (WebRequestContext.Localization.IsStaging)
            {
                if (!page.XpmMetadata.ContainsKey("CmsUrl"))
                {
                    page.XpmMetadata.Add("CmsUrl", SiteConfiguration.GetConfig("core.cmsurl", WebRequestContext.Localization));
                }
                return new MvcHtmlString(TridionMarkup.PageMarkup(page.XpmMetadata));
            }
            return null;
        }

        /// <summary>
        /// Render additional XPM include page markup
        /// </summary>
        /// <param name="page">The include Page Model</param>
        /// <param name="helper">Html Helper</param>
        /// <returns>The include page markup</returns>
        public override MvcHtmlString RenderIncludePageData(PageModel page, HtmlHelper helper)
        {
            if (WebRequestContext.Localization.IsStaging)
            {
                return helper.Partial("Partials/XpmButton", page);
            }
            return null;
        }
    }
}
