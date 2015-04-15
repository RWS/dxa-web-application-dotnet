using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.Mvc;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;

namespace Sdl.Web.Mvc.Html
{
    /// <summary>
    /// HtmlHelper extension methods for use in Views
    /// </summary>
    public static class HtmlHelperExtensions
    {
        /// <summary>
        /// Format a date using the appropriate localization culture
        /// </summary>
        /// <param name="htmlHelper">HtmlHelper</param>
        /// <param name="date">Date to format</param>
        /// <param name="format">Format string (default is "D")</param>
        /// <returns>Formatted date</returns>
        public static string Date(this HtmlHelper htmlHelper, DateTime? date, string format = "D")
        {
            return date != null ? ((DateTime)date).ToString(format, new CultureInfo(SiteConfiguration.GetConfig("core.culture", WebRequestContext.Localization.Path))) : null;
        }

        /// <summary>
        /// Show a text representation of the difference between a given date and now
        /// </summary>
        /// <param name="htmlHelper">HtmlHelper</param>
        /// <param name="date">The date to compare with the current date</param>
        /// <param name="format">Format string (default is "D")</param>
        /// <returns>Localized versions of "Today", "Yesterday", "X days ago" (for less than a week ago) or the formatted date</returns>
        public static string DateDiff(this HtmlHelper htmlHelper, DateTime? date, string format = "D")
        {
            if (date != null)
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

                return ((DateTime)date).ToString(format, new CultureInfo(SiteConfiguration.GetConfig("core.culture", WebRequestContext.Localization.Path)));
            }
            return null;
        }

        /// <summary>
        /// Read a configuration value
        /// </summary>
        /// <param name="htmlHelper">HtmlHelper</param>
        /// <param name="configName">The config key (eg core.cmsUrl)</param>
        /// <returns>The config value</returns>
        public static string Config(this HtmlHelper htmlHelper, string configName)
        {
            return SiteConfiguration.GetConfig(configName, WebRequestContext.Localization.Path);
        }

        /// <summary>
        /// Read a resource value
        /// </summary>
        /// <param name="htmlHelper">HtmlHelper</param>
        /// <param name="resourceName">The resource key (eg core.readMoreText)</param>
        /// <returns>The resource value, or key name if none found</returns>
        public static string Resource(this HtmlHelper htmlHelper, string resourceName)
        {
            return (string)Resource(htmlHelper.ViewContext.HttpContext, resourceName);
        }

        /// <summary>
        /// Read a resource string and format it with parameters
        /// </summary>
        /// <param name="htmlHelper">HtmlHelper</param>
        /// <param name="resourceName">The resource key (eg core.readMoreText)</param>
        /// <param name="parameters">Format parameters</param>
        /// <returns>The formatted resource value, or key name if none found</returns>
        public static string FormatResource(this HtmlHelper htmlHelper, string resourceName, params object[] parameters)
        {
            return String.Format(htmlHelper.Resource(resourceName), parameters);
        }        

        /// <summary>
        /// Read a resource string and format it with parameters
        /// </summary>
        /// <param name="httpContext">The HttpContext</param>
        /// <param name="resourceName">The resource key (eg core.readMoreText)</param>
        /// <param name="parameters">Format parameters</param>
        /// <returns>The formatted resource value, or key name if none found</returns>
        public static object FormatResource(this HttpContextBase httpContext, string resourceName, params object[] parameters)
        {
            return String.Format((string)httpContext.Resource(resourceName), parameters);
        }

        /// <summary>
        /// Read a resource value
        /// </summary>
        /// <param name="httpContext">The HttpContext</param>
        /// <param name="resourceName">The resource key (eg core.readMoreText)</param>
        /// <returns>The resource value, or key name if none found</returns>
        public static object Resource(this HttpContextBase httpContext, string resourceName)
        {
            return httpContext.GetGlobalResourceObject(CultureInfo.CurrentUICulture.ToString(), resourceName);
        }

        /// <summary>
        /// Convert a number into a filesize display value
        /// </summary>
        /// <param name="httpContext">The HttpContext</param>
        /// <param name="sizeInBytes">The file size in bytes</param>
        /// <returns>File size string (eg 132 MB)</returns>
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
                widthFactor = SiteConfiguration.MediaHelper.DefaultMediaFill;
            }

            //We read the container size (based on bootstrap grid) from the view bag
            //This means views can be independent of where they are rendered and do not
            //need to know their width
            TagBuilder builder = new TagBuilder("img");
            builder.Attributes.Add("src", SiteConfiguration.MediaHelper.GetResponsiveImageUrl(image.Url, aspect, widthFactor, containerSize));
            if (!String.IsNullOrEmpty(imgWidth))
            {
                builder.Attributes.Add("width", imgWidth);
            }
            builder.Attributes.Add("alt", image.AlternateText);
            builder.Attributes.Add("data-aspect", (Math.Truncate(aspect * 100) / 100).ToString());
            if (!String.IsNullOrEmpty(cssClass))
            {
                builder.Attributes.Add("class", cssClass);
            }
            return new MvcHtmlString(builder.ToString(TagRenderMode.SelfClosing));
        }

        /// <summary>
        /// Write out an media item with a responsive url
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="media">The media item to write out</param>
        /// <param name="widthFactor">The factor to apply to the width - can be % (eg "100%") or absolute (eg "120")</param>
        /// <param name="aspect">The aspect ratio for the image</param>
        /// <param name="cssClass">Css class to apply to img tag</param>
        /// <param name="containerSize">The size (in grid column units) of the containing element</param>
        /// <returns>Complete media markup with all required attributes</returns>
        public static MvcHtmlString Media(this HtmlHelper helper, MediaItem media, string widthFactor, double aspect, string cssClass = null, int containerSize = 0)
        {
            if (media == null)
            {
                return null;
            }
            //We read the container size (based on bootstrap grid) from the view bag
            //This means views can be independent of where they are rendered and do not
            //need to know their width
            if (containerSize == 0)
            {
                containerSize = helper.ViewBag.ContainerSize;
            }
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

        /// <summary>
        /// Write out an youtube video item
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="media">The video item to write out</param>
        /// <param name="widthFactor">The factor to apply to the width - can be % (eg "100%") or absolute (eg "120")</param>
        /// <param name="aspect">The aspect ratio for the video</param>
        /// <param name="cssClass">Css class to apply</param>
        /// <param name="containerSize">The size (in grid column units) of the containing element</param>
        /// <returns>Complete video markup with all required attributes</returns>
        public static MvcHtmlString YouTubeVideo(HtmlHelper helper, YouTubeVideo video, string widthFactor, double aspect, string cssClass, int containerSize)
        {
            if (video == null || String.IsNullOrEmpty(video.YouTubeId))
            {
                return null;
            }

            if (video.Url != null && SiteConfiguration.MediaHelper.ShowVideoPlaceholders)
            {
                //we have a placeholder image
                var placeholderImgUrl = SiteConfiguration.MediaHelper.GetResponsiveImageUrl(video.Url, aspect, widthFactor, containerSize);
                return new MvcHtmlString(GetYouTubePlaceholder(video.YouTubeId, placeholderImgUrl, video.Headline, cssClass));
            }

            return new MvcHtmlString(GetYouTubeEmbed(video.YouTubeId, cssClass));
        }

        /// <summary>
        /// Write out a download link
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="download">The download item to render</param>
        /// <returns></returns>
        public static MvcHtmlString Download(this HtmlHelper helper, Download download)
        {
            if (download == null || String.IsNullOrEmpty(download.Url))
            {
                return null;
            }

            // TODO: this does not contain any XPM markup
            // TODO: configurize the mime type to Font Awesome mapping
            // filetypes supported by http://fortawesome.github.io/Font-Awesome/icons/#file-type
            var mimeTypes = new Dictionary<string, string>
                {
                    {"application/ms-excel", "excel"},
                    {"application/pdf", "pdf"},
                    {"application/x-wav", "audio"},
                    {"audio/x-mpeg", "audio"},
                    {"application/msword", "word"},
                    {"text/rtf", "word"},
                    {"application/zip", "archive"},
                    {"image/gif", "image"},
                    {"image/jpeg", "image"},
                    {"image/png", "image"},
                    {"image/x-bmp", "image"},
                    {"text/plain", "text"},
                    {"text/css", "code"},
                    {"application/x-javascript", "code"},
                    {"application/ms-powerpoint", "powerpoint"},
                    {"video/vnd.rn-realmedia", "video"},
                    {"video/quicktime", "video"},
                    {"video/mpeg", "video"}
                }; 
            string fileType = null;
            if (mimeTypes.ContainsKey(download.MimeType))
            {
                fileType = mimeTypes[download.MimeType];
            }
            string iconClass = (fileType == null ? "fa-file" : String.Format("fa-file-{0}-o", fileType));
            string friendlyFileSize = helper.FriendlyFileSize(download.FileSize).ToString();
            string descriptionHtml = (!String.IsNullOrEmpty(download.Description) ? String.Format("<small>{0}</small>", download.Description) : String.Empty);
            // TODO: use partial view instead of hardcoding HTML
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
            return Media(helper, media, widthFactor, SiteConfiguration.MediaHelper.DefaultMediaAspect, cssClass);
        }
        
        public static MvcHtmlString Media(this HtmlHelper helper, MediaItem media, double aspect, string cssClass = null)
        {
            return Media(helper, media, null, aspect, cssClass);
        }

        public static string GetYouTubeUrl(string videoId)
        {
            return String.Format("https://www.youtube.com/embed/{0}?version=3&enablejsapi=1", videoId);
        }

        public static string GetYouTubeEmbed(string videoId, string cssClass = null)
        {
            TagBuilder builder = new TagBuilder("iframe");
            builder.Attributes.Add("src", GetYouTubeUrl(videoId));
            builder.Attributes.Add("id", SiteConfiguration.GetUniqueId("video"));
            builder.Attributes.Add("allowfullscreen", "true");
            builder.Attributes.Add("frameborder", "0");

            if (!String.IsNullOrEmpty(cssClass))
            {
                builder.Attributes.Add("class", cssClass);
            }

            return builder.ToString(TagRenderMode.SelfClosing);
        }

        public static string GetYouTubePlaceholder(string videoId, string imageUrl, string altText = null, string cssClass = null, string elementName = "div", bool xmlCompliant = false)
        {
            string closing = xmlCompliant ? "/" : String.Empty;

            // TODO: consider using partial view
            return String.Format("<{4} class=\"embed-video\"><img src=\"{1}\" alt=\"{2}\"{5}><button type=\"button\" data-video=\"{0}\" class=\"{3}\"><i class=\"fa fa-play-circle\"></i></button></{4}>", videoId, imageUrl, altText, cssClass, elementName, closing);
        }

        public static string GetResponsiveImageUrl(string url)
        {
            return GetResponsiveImageUrl(url, SiteConfiguration.MediaHelper.DefaultMediaFill);
        }

        public static string GetResponsiveImageUrl(string url, double aspect, int containerSize = 0)
        {
            return SiteConfiguration.MediaHelper.GetResponsiveImageUrl(url, aspect, SiteConfiguration.MediaHelper.DefaultMediaFill, containerSize);
        }

        public static string GetResponsiveImageUrl(string url, string widthFactor, int containerSize = 0)
        {
            return SiteConfiguration.MediaHelper.GetResponsiveImageUrl(url, SiteConfiguration.MediaHelper.DefaultMediaAspect, widthFactor, containerSize);
        }
    }
}
