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
        /*public static string ResponsiveImageUrl(this HtmlHelper helper, string url, int baseSize, bool fixHeight = false)
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
        }*/

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

        public static MvcHtmlString Image(this HtmlHelper helper, Image image, double aspect = 1.62, double fillFactor = 1, string cssClass = null)//TODO add sizing hint?
        {
            if (image==null || String.IsNullOrEmpty(image.Url))
            {
                return null;
            }
            //We read the container size (based on 12 column grid) from the view bag
            //This means views can be independent of where they are rendered
            int containerSize = helper.ViewBag.ContainerSize;
            if (containerSize == 0)
            {
                containerSize = 12;//default is full width
            }
            switch (WebRequestContext.ScreenWidth)
            {
                case ScreenWidth.ExtraSmall:
                     //Extra small screens are only one column
                    containerSize = 12;
                    break;
                case ScreenWidth.Small:
                    //Small screens are max 2 columns
                    containerSize = (containerSize <= 6 ? 6 : 12);
                    break;
            }
            int cols = 12 / containerSize;
            //TODO - should we make padding configurable?
            int padding = (cols - 1) * 20;
            //Get the max possible width
            double width = WebRequestContext.MaxMediaWidth;
            //Factor the max possible width by the container size and remove padding
            width = (fillFactor * containerSize * width / 12 ) - padding;
            //TODO read these 'limit points' from config published from CMS (also used to customize LESS/CSS)
            List<int> limits = new List<int>{ 160, 320, 640, 1024, 2048 };
            //Round the width to the nearest set limit point - important as we do not want 
            //to swamp the cache with lots of different sized versions of the same image
            for (int i = 0; i < limits.Count; i++)
            {
                if (width <= limits[i])
                {
                    width = limits[i];
                    break;
                }
            }
            //Height is calculated from the aspect ratio
            double height = width / aspect;
            //TODO configure this somewhere
            string url = String.Format("/cid/scale/{0}x{1}/source/site{2}", Math.Ceiling(width), Math.Ceiling(height), image.Url);
            TagBuilder builder = new TagBuilder("img");
            builder.Attributes.Add("src", url);
            builder.Attributes.Add("alt", image.AlternateText);
            if (!String.IsNullOrEmpty(cssClass))
            {
                builder.Attributes.Add("class", cssClass);
            }
            return new MvcHtmlString(builder.ToString(TagRenderMode.SelfClosing));
        }
    }
}
