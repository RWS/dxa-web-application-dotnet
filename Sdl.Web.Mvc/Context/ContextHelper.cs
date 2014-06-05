using Sdl.Web.Mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Sdl.Web.Mvc.Context
{
    public static class ContextHelper
    {
        #region HtmlHelpers

        //The Golden Ratio is our default aspect
        public const double DEFAULT_MEDIA_ASPECT = 1.62;
        //The default fill for media is 100% of containing element
        public const string DEFAULT_MEDIA_FILL = "100%";

        /// <summary>
        /// Write out an img tag with a responsive image url
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="image">The image to write out</param>
        /// <param name="aspect">The aspect ratio for the image</param>
        /// <param name="widthFactor">The factor to apply to the width - can be % (eg "100%") or absolute (eg "120")</param>
        /// <param name="cssClass">Css class to apply to img tag</param>
        /// <returns>Complete img tag with all required attributes</returns>
        public static MvcHtmlString Image(this HtmlHelper helper, Image image, double aspect, string widthFactor, string cssClass = null)
        {
            if (image == null || String.IsNullOrEmpty(image.Url))
            {
                return null;
            }
            //We read the container size (based on bootstrap grid) from the view bag
            //This means views can be independent of where they are rendered and do not
            //need to know their width
            int containerSize = helper.ViewBag.ContainerSize;
            TagBuilder builder = new TagBuilder("img");
            builder.Attributes.Add("src", ContextHelper.GetResponsiveImageUrl(image.Url, aspect, widthFactor, containerSize));
            builder.Attributes.Add("alt", image.AlternateText);
            if (!String.IsNullOrEmpty(cssClass))
            {
                builder.Attributes.Add("class", cssClass);
            }
            return new MvcHtmlString(builder.ToString(TagRenderMode.SelfClosing));
        }
        //Variations for more concise usage in Views
        public static MvcHtmlString Image(this HtmlHelper helper, Image image)
        {
            return Image(helper, image, DEFAULT_MEDIA_FILL);
        }
        public static MvcHtmlString Image(this HtmlHelper helper, Image image, string widthFactor, string cssClass = null)
        {
            return Image(helper, image, DEFAULT_MEDIA_ASPECT, widthFactor, cssClass);
        }
        public static MvcHtmlString Image(this HtmlHelper helper, Image image, double aspect, string cssClass = null)
        {
            return Image(helper, image, aspect, DEFAULT_MEDIA_FILL, cssClass);
        }
        #endregion

        /// <summary>
        /// Helper method to get a responsive image URL
        /// </summary>
        /// <param name="image">The original image URL</param>
        /// <param name="aspect">The aspect ratio</param>
        /// <param name="widthFactor">The factor to apply to the width - can be % (eg "100%") or absolute (eg "120")</param>
        /// <param name="containerSize">The size (in grid column units) of the containing element</param>
        /// <returns></returns>
        public static string GetResponsiveImageUrl(string url, double aspect, string widthFactor, int containerSize = 0)
        {
            if (containerSize == 0)
            {
                //default is full width
                containerSize = ContextConfiguration.GridSize;
            }
            double width = 0;
            //For absolute fill factors, we should have a number
            if (!widthFactor.EndsWith("%"))
            {
                if (!Double.TryParse(widthFactor, out width))
                {
                    Log.Warn("Invalid width factor (\"{0}\") when resizing image, defaulting to {1}", widthFactor, DEFAULT_MEDIA_FILL);
                    //Change the fill factor to the default (100%)
                    widthFactor = DEFAULT_MEDIA_FILL;
                }
                else
                {
                    width = width * WebRequestContext.ContextEngine.Device.PixelRatio;
                }
            }
            //For percentage fill factors, we need to do some calculation of container size etc.
            if (widthFactor.EndsWith("%"))
            {
                int fillFactor = Int32.Parse(DEFAULT_MEDIA_FILL.Substring(0,widthFactor.Length-1));
                if (!Int32.TryParse(widthFactor.Substring(0, widthFactor.Length - 1), out fillFactor))
                {
                    Log.Warn("Invalid width factor (\"{0}\") when resizing image, defaulting to {1}", widthFactor, DEFAULT_MEDIA_FILL);
                }
                //TODO make the screen width behaviour configurable?
                switch (WebRequestContext.ScreenWidth)
                {
                    case ScreenWidth.ExtraSmall:
                        //Extra small screens are only one column
                        containerSize = ContextConfiguration.GridSize;
                        break;
                    case ScreenWidth.Small:
                        //Small screens are max 2 columns
                        containerSize = (containerSize <= ContextConfiguration.GridSize / 2 ? ContextConfiguration.GridSize / 2 : ContextConfiguration.GridSize);
                        break;
                }
                int cols = ContextConfiguration.GridSize / containerSize;
                //TODO - should we make padding configurable?
                int padding = (cols - 1) * 20;
                //Get the max possible width
                width = WebRequestContext.MaxMediaWidth;
                //Factor the max possible width by the fill factor and container size and remove padding
                width = (fillFactor * containerSize * width / (ContextConfiguration.GridSize * 100)) - padding;
                //Round the width to the nearest set limit point - important as we do not want 
                //to swamp the cache with lots of different sized versions of the same image
                for (int i = 0; i < ContextConfiguration.ImageWidths.Count; i++)
                {
                    if (width <= ContextConfiguration.ImageWidths[i])
                    {
                        width = ContextConfiguration.ImageWidths[i];
                        break;
                    }
                }
            }
            //Height is calculated from the aspect ratio
            double height = width / aspect;
            //Build the URL
            return String.Format(ContextConfiguration.ImageResizeUrl, ContextConfiguration.ImageResizeRoute, Math.Ceiling(width), Math.Ceiling(height), url);
        }
        public static string GetResponsiveImageUrl(string url)
        {
            return GetResponsiveImageUrl(url, DEFAULT_MEDIA_FILL);
        }
        public static string GetResponsiveImageUrl(string url, double aspect, int containerSize = 0)
        {
            return GetResponsiveImageUrl(url, aspect, DEFAULT_MEDIA_FILL, containerSize);
        }
        public static string GetResponsiveImageUrl(string url, string widthFactor, int containerSize = 0)
        {
            return GetResponsiveImageUrl(url, DEFAULT_MEDIA_ASPECT, widthFactor, containerSize);
        }
        
    }
}
