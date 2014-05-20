using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web;
using System.Globalization;
using Sdl.Web.Mvc.Models;
using System.Web.Mvc;

namespace Sdl.Web.Mvc.Html
{
    public static class HtmlHelperExtensions
    {
        public static string ResponsiveImageUrl(this HtmlHelper helper, string url, int baseSize, bool fixHeight = false)
        {
            double maxWidth = WebRequestContext.MaxMediaWidth;
            int factor = (int)Math.Ceiling(maxWidth / baseSize);
            factor = factor > 4 ? 8 : factor==3 ? 4 : factor;//factor is 1x 2x 4x or 8x our base (small screen) width - as we only want to support 4 versions of an image, and want to cap it at base x 8
            factor = WebRequestContext.ContextEngine.Device.PixelRatio == 1 && factor > 4 ? 4 : factor;//max x4 for pixel ratio of 1 
            int width = factor * baseSize;
            return String.Format("/cid/fit/{0}{1}/source/site{2}", fixHeight ? "x" : "", width, url);
        }

        public static string FixedSizeImageUrl(this HtmlHelper helper, string url, int size, bool fixHeight = true, double aspect = 1.62)
        {
            size = size * WebRequestContext.ContextEngine.Device.PixelRatio;
            int height = fixHeight ? size : (int) (size / aspect);
            int width = fixHeight ? (int)(size * aspect) : size;
            return String.Format("/cid/scale/{0}x{1}/source/site{2}", width, height, url);
        }

        public static string Date(this HtmlHelper htmlHelper, DateTime? date, string format = "D")
        {
            return date!=null ? ((DateTime)date).ToString(format, new CultureInfo(Configuration.GetConfig("site.culture"))) : null;
        }

        public static string Resource(this HtmlHelper htmlHelper, string resourceName)
        {
            return (string)Resource(htmlHelper.ViewContext.HttpContext, resourceName);
        }

        public static object Resource(this HttpContextBase httpContext, string resourceName)
        {
            return httpContext.GetGlobalResourceObject(CultureInfo.CurrentUICulture.ToString(), resourceName);
        }

        public static MvcHtmlString Image(this HtmlHelper helper, Image image, double aspect = 1.62)//TODO add sizing hint?
        {
            int containerSize = helper.ViewBag.ContainerSize;
            if (containerSize == 0)
            {
                containerSize = 12;
            }
            switch (WebRequestContext.ScreenWidth)
            {
                case ScreenWidth.ExtraSmall:
                    containerSize = 12;
                    break;
                case ScreenWidth.Small:
                    containerSize = (containerSize <= 6 ? 6 : 12);
                    break;
            }
            int cols = 12 / containerSize;
            int padding = (cols - 1) * 20;
            double max = WebRequestContext.MaxMediaWidth;
            max = (containerSize * max / 12 ) - padding;
            if (max < 160)
            {
                max = 160;
            }
            else if (max < 320)
            {
                max = 320;
            }
            else if (max < 640)
            {
                max = 640;
            }
            else if (max < 1024)
            {
                max = 1024;
            }
            else if (max < 2048)
            {
                max = 2048;
            }
            double height = max / aspect;
            string url = String.Format("/cid/scale/{0}x{1}/source/site{2}", Math.Ceiling(max), Math.Ceiling(height), image.Url);
            TagBuilder builder = new TagBuilder("img");
            builder.Attributes.Add("src", url);
            builder.Attributes.Add("alt", image.AlternateText);
            return new MvcHtmlString(builder.ToString(TagRenderMode.SelfClosing));
        }
    }
}
