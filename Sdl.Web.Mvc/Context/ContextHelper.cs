using Sdl.Web.Mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Mvc.Html;

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
        public static MvcHtmlString Image(this HtmlHelper helper, Image image, string widthFactor, double aspect, string cssClass = null, int containerSize = 0)
        {
            if (image == null || String.IsNullOrEmpty(image.Url))
            {
                return null;
            }
            //We read the container size (based on bootstrap grid) from the view bag
            //This means views can be independent of where they are rendered and do not
            //need to know their width
            TagBuilder builder = new TagBuilder("img");
            builder.Attributes.Add("src", ContextHelper.GetResponsiveImageUrl(image.Url, aspect, widthFactor, containerSize));
            builder.Attributes.Add("width", widthFactor);
            builder.Attributes.Add("alt", image.AlternateText);
            if (!String.IsNullOrEmpty(cssClass))
            {
                builder.Attributes.Add("class", cssClass);
            }
            return new MvcHtmlString(builder.ToString(TagRenderMode.SelfClosing));
        }

        public static MvcHtmlString Media(this HtmlHelper helper, MediaItem media, string widthFactor, double aspect, string cssClass = null)
        {
            if (media == null)
            {
                return null;
            }
            //We read the container size (based on bootstrap grid) from the view bag
            //This means views can be independent of where they are rendered and do not
            //need to know their width
            int containerSize = helper.ViewBag.ContainerSize;
            if (media is Image)
            {
                return Image(helper, (Image)media, widthFactor, aspect, cssClass, containerSize);
            }
            if (media is YouTubeVideo)
            {
                return YouTubeVideo(helper, (YouTubeVideo)media, widthFactor, aspect, cssClass, containerSize);
            }
            if (media is Download)
            {
                return Download(helper, (Download)media);
            }
            return null;
        }

        public static MvcHtmlString YouTubeVideo(HtmlHelper helper, YouTubeVideo video, string widthFactor, double aspect, string cssClass, int containerSize)
        {
            if (video == null || String.IsNullOrEmpty(video.YouTubeId))
            {
                return null;
            }
            TagBuilder builder = new TagBuilder("iframe");
            builder.Attributes.Add("src", ContextHelper.GetYouTubeUrl(video.YouTubeId));
            builder.Attributes.Add("id", Configuration.GetUniqueId("video"));
            builder.Attributes.Add("allowfullscreen", "true");
            builder.Attributes.Add("frameborder", "0");
            if (!String.IsNullOrEmpty(cssClass))
            {
                builder.Attributes.Add("class", cssClass);
            }
            return new MvcHtmlString(builder.ToString(TagRenderMode.SelfClosing));
        }

        public static MvcHtmlString Download(this HtmlHelper helper, Download download)
        {
            if (download == null || String.IsNullOrEmpty(download.Url))
            {
                return null;
            }
            TagBuilder builder = new TagBuilder("a");
            builder.Attributes.Add("href", download.Url);
            builder.SetInnerText((download.Description ?? "download"));
            return new MvcHtmlString(builder.ToString());
        }

        public static MvcHtmlString Media(this HtmlHelper helper, MediaItem media)
        {
            return Media(helper, media, DEFAULT_MEDIA_FILL);
        }
        public static MvcHtmlString Media(this HtmlHelper helper, MediaItem media, string widthFactor, string cssClass = null)
        {
            return Media(helper, media, widthFactor, DEFAULT_MEDIA_ASPECT, cssClass);
        }
        public static MvcHtmlString Media(this HtmlHelper helper, MediaItem media, double aspect, string cssClass = null)
        {
            return Media(helper, media, DEFAULT_MEDIA_FILL, aspect, cssClass);
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
                int fillFactor;
                if (!Int32.TryParse(widthFactor.Substring(0, widthFactor.Length - 1), out fillFactor))
                {
                    Log.Warn("Invalid width factor (\"{0}\") when resizing image, defaulting to {1}", widthFactor, DEFAULT_MEDIA_FILL);
                }
                if (fillFactor == 0)
                {
                    fillFactor = Int32.Parse(DEFAULT_MEDIA_FILL.Substring(0, DEFAULT_MEDIA_FILL.Length - 1));
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

        public static string GetYouTubeUrl(string videoId)
        {
            return String.Format("https://www.youtube.com/embed/{0}?version=3&enablejsapi=1", videoId);
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
