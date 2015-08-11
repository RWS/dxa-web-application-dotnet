using System;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Sdl.Web.Mvc.Html
{
    [Obsolete("Renderers are deprecated in DXA 1.1. Rendering should be done using DXA 1.1 HtmlHelper extension methods.")]
#pragma warning disable 618
    public class BaseRenderer : IRenderer
    {
        public IContentResolver ContentResolver { get; set; }

        public BaseRenderer()
        { 
        }

        public BaseRenderer(IContentResolver resolver)
        {
            ContentResolver = resolver;
        }


        public virtual MvcHtmlString RenderEntity(object item, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null)
        {
            Log.Warn("IRenderer.RenderEntity({0}) is used but deprecated in DXA 1.1. Use @Html.DxaEntity instead.", item);

            EntityModel entity = (EntityModel)item;
            if (entity == null)
            {
                return MvcHtmlString.Empty;
            }
            if ((excludedItems != null) && excludedItems.Contains(entity.MvcData.ViewName))
            {
                return MvcHtmlString.Empty;
            }

            return helper.DxaEntity(entity, containerSize);
        }

        public virtual MvcHtmlString RenderRegion(IRegion region, HtmlHelper helper, int containerSize = 0, List<string> excludedItems = null)
        {
            Log.Warn("IRenderer.RenderRegion({0}) is used but deprecated in DXA 1.1. Use @Html.DxaRegion instead.", region);

            if (region == null || (excludedItems != null && excludedItems.Contains(region.Name)))
            {
                return MvcHtmlString.Empty;
            }

            return helper.DxaRegion((RegionModel)region, containerSize);
        }

        public virtual MvcHtmlString RenderPageData(IPage page, HtmlHelper helper)
        {
            Log.Warn("IRenderer.RenderPageData({0}) is used but deprecated in DXA 1.1. Use @Html.DxaPageMarkup instead.", page);

            return helper.DxaPageMarkup();
        }
    }
#pragma warning restore 618
}
