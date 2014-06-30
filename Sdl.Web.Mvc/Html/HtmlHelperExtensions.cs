using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Sdl.Web.Models;
using Sdl.Web.Common.Interfaces;

namespace Sdl.Web.Mvc.Html
{
    public static class HtmlHelperExtensions
    {
        public static string Date(this HtmlHelper htmlHelper, DateTime? date, string format = "D")
        {
            return date != null ? ((DateTime)date).ToString(format, new CultureInfo(Configuration.GetConfig("core.culture"))) : null;
        }

        public static string DateDiff(this HtmlHelper htmlHelper, DateTime? date, string format = "D")
        {
            //TODO make the text come from resources
            if (date!=null)
            {
                int dayDiff = (int)(DateTime.Now.Date - ((DateTime)date).Date).TotalDays;
                if (dayDiff <= 0)
                {
                    return htmlHelper.Resource("core.todayText");
                }
                if (dayDiff == 1)
                {
                    return htmlHelper.Resource("core.yesterdayText");
                }
                if (dayDiff <= 7)
                {
                    return String.Format(htmlHelper.Resource("core.xDaysAgoText"), dayDiff);
                }
                else
                {
                    return ((DateTime)date).ToString(format, new CultureInfo(Configuration.GetConfig("core.culture")));
                }
            }
            return null;
        }

        public static string FormatResource(this HtmlHelper htmlHelper, string resourceName, params object[] parameters)
        {
            return String.Format((string)htmlHelper.Resource(resourceName),parameters);
        }

        public static string Resource(this HtmlHelper htmlHelper, string resourceName)
        {
            return (string)Resource(htmlHelper.ViewContext.HttpContext, resourceName);
        }

        public static object FormatResource(this HttpContextBase httpContext, string resourceName, params object[] parameters)
        {
            return String.Format((string)httpContext.Resource(resourceName), parameters);
        }

        public static object Resource(this HttpContextBase httpContext, string resourceName)
        {
            return httpContext.GetGlobalResourceObject(CultureInfo.CurrentUICulture.ToString(), resourceName);
        }

        public static object FriendlyFileSize(this HtmlHelper httpContext, long sizeInBytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            double len = sizeInBytes;
            int order = 0;
            while (len >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                len = len / 1024;
            }

            return String.Format("{0} {1}", Math.Ceiling(len), sizes[order]);
        }
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
            builder.Attributes.Add("src", Configuration.MediaHelper.GetResponsiveImageUrl(image.Url, aspect, widthFactor, containerSize));
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
            if (video.Url != null && Configuration.MediaHelper.ShowVideoPlaceholders)
            {
                //we have a placeholder image
                var placeholderImgUrl = Configuration.MediaHelper.GetResponsiveImageUrl(video.Url, aspect, widthFactor, containerSize);
                return new MvcHtmlString(GetYouTubePlaceholder(video.YouTubeId,placeholderImgUrl,video.Headline,cssClass));
            }
            else
            {
                TagBuilder builder = new TagBuilder("iframe");
                builder.Attributes.Add("src", GetYouTubeUrl(video.YouTubeId));
                builder.Attributes.Add("id", Configuration.GetUniqueId("video"));
                builder.Attributes.Add("allowfullscreen", "true");
                builder.Attributes.Add("frameborder", "0");
                if (!String.IsNullOrEmpty(cssClass))
                {
                    builder.Attributes.Add("class", cssClass);
                }
                return new MvcHtmlString(builder.ToString(TagRenderMode.SelfClosing));
            }
        }

        public static MvcHtmlString Download(this HtmlHelper helper, Download download)
        {
            if (download == null || String.IsNullOrEmpty(download.Url))
            {
                return null;
            }

            //todo this does not contain any XPM markup
            //todo configurize the mime type to Font Awesome mapping
            var mimeTypes = new Dictionary<string, string>(); // filetypes supported by http://fortawesome.github.io/Font-Awesome/icons/#file-type
            mimeTypes.Add("application/ms-excel", "excel");
            mimeTypes.Add("application/pdf", "pdf");
            mimeTypes.Add("application/x-wav", "audio");
            mimeTypes.Add("audio/x-mpeg", "audio");
            mimeTypes.Add("application/msword", "word");
            mimeTypes.Add("text/rtf", "word");
            mimeTypes.Add("application/zip", "archive");
            mimeTypes.Add("image/gif", "image");
            mimeTypes.Add("image/jpeg", "image");
            mimeTypes.Add("image/png", "image");
            mimeTypes.Add("image/x-bmp", "image");
            mimeTypes.Add("text/plain", "text");
            mimeTypes.Add("text/css", "code");
            mimeTypes.Add("application/x-javascript", "code");
            mimeTypes.Add("application/ms-powerpoint", "powerpoint");
            mimeTypes.Add("video/vnd.rn-realmedia", "video");
            mimeTypes.Add("video/quicktime", "video");
            mimeTypes.Add("video/mpeg", "video");
            string fileType = null;
            if (mimeTypes.ContainsKey(download.MimeType))
            {
                fileType = mimeTypes[download.MimeType];
            }
            string iconClass = (fileType == null ? "fa-file" : String.Format("fa-file-{0}-o", fileType));
            string friendlyFileSize = helper.FriendlyFileSize(download.FileSize).ToString();
            string descriptionHtml = (!String.IsNullOrEmpty(download.Description) ? String.Format("<small>{0}</small>", download.Description) : "");
            string downloadHtml = String.Format(@"
                <div class=""download-list"">
                    <i class=""fa {0}""></i>
                    <div>
                        <a href=""{1}"">{2}</a> <small class=""size"">({3})</small>
                        {4}
                    </div>
                </div>", iconClass, download.Url, download.FileName, friendlyFileSize, descriptionHtml);
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

        public static string GetYouTubeUrl(string videoId)
        {
            return String.Format("https://www.youtube.com/embed/{0}?version=3&enablejsapi=1", videoId);
        }

        public static string GetYouTubePlaceholder(string videoId, string imageUrl, string altText = null, string cssClass = null)
        {
            return String.Format("<div class=\"embed-video\"><img src=\"{1}\" alt=\"{2}\"><button type=\"button\" data-video=\"{0}\" class=\"{3}\"><i class=\"fa fa-play-circle\"></i></button></div>", videoId, imageUrl, altText, cssClass);
        }

        public static string GetResponsiveImageUrl(string url)
        {
            return GetResponsiveImageUrl(url, DEFAULT_MEDIA_FILL);
        }
        public static string GetResponsiveImageUrl(string url, double aspect, int containerSize = 0)
        {
            return Configuration.MediaHelper.GetResponsiveImageUrl(url, aspect, DEFAULT_MEDIA_FILL, containerSize);
        }
        public static string GetResponsiveImageUrl(string url, string widthFactor, int containerSize = 0)
        {
            return Configuration.MediaHelper.GetResponsiveImageUrl(url, DEFAULT_MEDIA_ASPECT, widthFactor, containerSize);
        }


    }
}
