using Sdl.Web.Common.Interfaces;
using Sdl.Web.Mvc.Html;
using Sdl.Web.Models;
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
        public static IMediaHelper MediaHelper { get; set; }
        /// <summary>
        /// Write out an img tag with a responsive image url
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="image">The image to write out</param>
        /// <param name="aspect">The aspect ratio for the image</param>
        /// <param name="widthFactor">The factor to apply to the width - can be % (eg "100%") or absolute (eg "120")</param>
        /// <param name="cssClass">Css class to apply to img tag</param>
        /// <param name="containerSize">The size (in grid column units) of the containing element</param>
        /// <returns>Complete img tag with all required attributes</returns>
        public static MvcHtmlString Image(this HtmlHelper helper, Image image, string widthFactor, double aspect, string cssClass = null, int containerSize = 0)
        {
            if (image == null || String.IsNullOrEmpty(image.Url))
            {
                return null;
            }
            
            string imgWidth = widthFactor;
            if (widthFactor == null)
            {
                widthFactor = DEFAULT_MEDIA_FILL;
            }

            //We read the container size (based on bootstrap grid) from the view bag
            //This means views can be independent of where they are rendered and do not
            //need to know their width
            TagBuilder builder = new TagBuilder("img");
            builder.Attributes.Add("src", MediaHelper.GetResponsiveImageUrl(image.Url, aspect, widthFactor, containerSize));
            if (!String.IsNullOrEmpty(imgWidth))
            {
                builder.Attributes.Add("width", imgWidth);
            }
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
            builder.Attributes.Add("width", widthFactor);
            builder.Attributes.Add("height", Math.Max(MediaHelper.GetResponsiveHeight(widthFactor, 1.78, containerSize), 175).ToString());
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

            //todo this does not contain any XPM markup
            string friendlyFileSize = helper.FriendlyFileSize(download.FileSize).ToString();
            string descriptionHtml = (!String.IsNullOrEmpty(download.Description) ? String.Format("<small>{0}</small>", download.Description) : "");
            string downloadHtml = String.Format(@"
                <div class=""download-list"">
                    <i class=""fa fa-file""></i>
                    <div>
                        <a href=""{0}"">{1}</a> <small class=""size"">({2})</small>
                        {3}
                    </div>
                </div>", download.Url, download.FileName, friendlyFileSize, descriptionHtml);
            return new MvcHtmlString(downloadHtml);
        }

        public static MvcHtmlString Media(this HtmlHelper helper, MediaItem media)
        {
            return Media(helper, media, null);
        }
        public static MvcHtmlString Media(this HtmlHelper helper, MediaItem media, string widthFactor, string cssClass = null)
        {
            return Media(helper, media, widthFactor, DEFAULT_MEDIA_ASPECT, cssClass);
        }
        public static MvcHtmlString Media(this HtmlHelper helper, MediaItem media, double aspect, string cssClass = null)
        {
            return Media(helper, media, null, aspect, cssClass);
        }
        #endregion

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
            return MediaHelper.GetResponsiveImageUrl(url, aspect, DEFAULT_MEDIA_FILL, containerSize);
        }
        public static string GetResponsiveImageUrl(string url, string widthFactor, int containerSize = 0)
        {
            return MediaHelper.GetResponsiveImageUrl(url, DEFAULT_MEDIA_ASPECT, widthFactor, containerSize);
        }

    }
}
