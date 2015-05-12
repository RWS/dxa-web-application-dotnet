using System;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Sdl.Web.Mvc.Html
{
    // TODO TSI-788: [Obsolete("Renderers are not used in DXA 1.1.")]
#pragma warning disable 618
    public abstract class BaseRenderer : IRenderer
    {
        public IContentResolver ContentResolver { get; set; }

        protected BaseRenderer()
        { 
        }

        protected BaseRenderer(IContentResolver resolver)
        {
            ContentResolver = resolver;
        }



        public abstract MvcHtmlString RenderEntity(EntityModel entity, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null);
        public abstract MvcHtmlString RenderRegion(RegionModel region, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null);
        public abstract MvcHtmlString RenderPageData(PageModel page, HtmlHelper helper);
        public abstract MvcHtmlString RenderIncludePageData(PageModel page, HtmlHelper helper);

        #region IRenderer Members

        MvcHtmlString IRenderer.RenderEntity(object item, HtmlHelper helper, int containerSize, List<string> excludedItems)
        {
            return RenderEntity((EntityModel) item, helper, containerSize, excludedItems);
        }

        MvcHtmlString IRenderer.RenderRegion(IRegion region, HtmlHelper helper, int containerSize, List<string> excludedItems)
        {
            return RenderRegion((RegionModel) region, helper, containerSize, excludedItems);
        }

        MvcHtmlString IRenderer.RenderPageData(IPage page, HtmlHelper helper)
        {
            return RenderPageData((PageModel) page, helper);
        }

        MvcHtmlString IRenderer.RenderIncludePageData(IPage page, HtmlHelper helper)
        {
            return RenderIncludePageData((PageModel) page, helper);
        }

        #endregion
    }
#pragma warning restore 618
}
