using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Tridion.Markup;

namespace Sdl.Web.Mvc.Html
{
    /// <summary>
    /// <see cref="HtmlHelper"/> extension methods for use in (Razor) Views.
    /// </summary>
    /// <remarks>
    /// These extension methods are available on the built-in <c>@Html</c> object.
    /// For example: <code>@Html.DxaRegions(exclude: "Logo")</code>
    /// </remarks>
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
            return date != null ? ((DateTime)date).ToString(format, WebRequestContext.Localization.CultureInfo) : null;
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

                return ((DateTime)date).ToString(format, WebRequestContext.Localization.CultureInfo);
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
            return SiteConfiguration.GetConfig(configName, WebRequestContext.Localization);
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
            builder.Attributes.Add("data-aspect", (Math.Truncate(aspect * 100) / 100).ToString(CultureInfo.InvariantCulture));
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
        /// <param name="video">The video item to write out</param>
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

        #region Region/Entity rendering extension methods
        /// <summary>
        /// Renders a given Entity Model.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance on which the extension method operates.</param>
        /// <param name="entity">The Entity to render.</param>
        /// <param name="containerSize">TODO</param>
        /// <returns>The rendered HTML or an empty string if <paramref name="entity"/> is <c>null</c>.</returns>
        public static MvcHtmlString DxaEntity(this HtmlHelper htmlHelper, EntityModel entity, int containerSize = 0)
        {
            if (entity == null)
            {
                return MvcHtmlString.Empty;
            }

            if (containerSize == 0)
            {
                containerSize = SiteConfiguration.MediaHelper.GridSize;
            }

            Log.Debug("Rendering Entity [{0}] (containerSize: {1})", entity, containerSize);

            MvcData mvcData = entity.MvcData;
            var parameters = new RouteValueDictionary();
            int parentContainerSize = htmlHelper.ViewBag.ContainerSize;
            if (parentContainerSize == 0)
            {
                parentContainerSize = SiteConfiguration.MediaHelper.GridSize;
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
            MvcHtmlString result = htmlHelper.Action(mvcData.ActionName, mvcData.ControllerName, parameters);
            if (WebRequestContext.IsPreview)
            {
                // TODO TSI-773: don't parse entity if this is in an include page (not rendered directly, so !WebRequestContext.IsInclude)
                result = new MvcHtmlString(TridionMarkup.ParseEntity(result.ToString()));
            }
            return result;
        }

        /// <summary>
        /// Renders all Entities in the current Region Model.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance on which the extension method operates.</param>
        /// <param name="containerSize">TODO</param>
        /// <returns>The rendered HTML.</returns>
        /// <remarks>This method will throw an exception if the current Model does not represent a Region.</remarks>
        public static MvcHtmlString DxaEntities(this HtmlHelper htmlHelper, int containerSize = 0)
        {
            RegionModel region = (RegionModel) htmlHelper.ViewData.Model;

            StringBuilder resultBuilder = new StringBuilder();
            foreach (EntityModel entity in region.Entities)
            {
                resultBuilder.Append(htmlHelper.DxaEntity(entity, containerSize));
            }
            return new MvcHtmlString(resultBuilder.ToString());
        }

        /// <summary>
        /// Renders a given Region Model
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance on which the extension method operates.</param>
        /// <param name="region">The Region Model to render. This object determines the View that will be used.</param>
        /// <param name="containerSize">TODO</param>
        /// <returns>The rendered HTML or an empty string if <paramref name="region"/> is <c>null</c>.</returns>
        public static MvcHtmlString DxaRegion(this HtmlHelper htmlHelper, RegionModel region, int containerSize = 0)
        {
            if (region == null)
            {
                return MvcHtmlString.Empty;
            }

            if (containerSize == 0)
            {
                containerSize = SiteConfiguration.MediaHelper.GridSize;
            }

            Log.Debug("Rendering Region '{0}' (containerSize: {1})", region.Name, containerSize);

            MvcData mvcData = region.MvcData;
            MvcHtmlString result = htmlHelper.Action(mvcData.ActionName, mvcData.ControllerName, new { Region = region, containerSize = containerSize, area = mvcData.ControllerAreaName });

            if (WebRequestContext.IsPreview)
            {
                // TODO TSI-773: don't parse region if this is a region in an include page (not rendered directly, so !WebRequestContext.IsInclude)
                result = new MvcHtmlString(TridionMarkup.ParseRegion(result.ToString(), WebRequestContext.Localization));
            }
            return result;
        }

        /// <summary>
        /// Renders a Region (of the current Page Model) with a given name.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance on which the extension method operates.</param>
        /// <param name="regionName">The name of the Region to render. This object determines the View that will be used.</param>
        /// <param name="emptyViewName">
        /// The name of the View to use when no Region with the given name is found in the Page Model (i.e. no Entities exist in the given Region). 
        /// If <c>null</c> (the default) then nothing will be rendered in that case. 
        /// </param>
        /// <param name="containerSize">TODO</param>
        /// <returns>The rendered HTML or an empty string if no Region with a given name is found and <paramref name="emptyViewName"/> is <c>null</c>.</returns>
        /// <remarks>This method will throw an exception if the current Model does not represent a Page.</remarks>
        public static MvcHtmlString DxaRegion(this HtmlHelper htmlHelper, string regionName, string emptyViewName = null, int containerSize = 0)
        {
            // TODO TSI-779: support nested Regions
            PageModel page = (PageModel) htmlHelper.ViewData.Model;
            RegionModel region;
            if (!page.Regions.TryGetValue(regionName, out region))
            {
                Log.Debug("Region '{0}' not found. Using empy View '{1}'.", regionName, emptyViewName);
                if (emptyViewName == null)
                {
                    return MvcHtmlString.Empty;
                }
                region = new RegionModel(regionName, emptyViewName);
            }

            return htmlHelper.DxaRegion(region, containerSize);
        }

        /// <summary>
        /// Renders all Regions (of the current Page Model), except the ones with given names.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance on which the extension method operates.</param>
        /// <param name="exclude">The (comma separated) name(s) of the Regions to exclude. Can be <c>null</c> (the default) to render all Regions.</param>
        /// <param name="containerSize">TODO</param>
        /// <returns>The rendered HTML.</returns>
        /// <remarks>This method will throw an exception if the current Model does not represent a Page.</remarks>
        public static MvcHtmlString DxaRegions(this HtmlHelper htmlHelper, string exclude = null, int containerSize = 0)
        {
            // TODO TSI-779: support nested Regions
            PageModel page = (PageModel)htmlHelper.ViewData.Model;

            IEnumerable<RegionModel> regions;
            if (string.IsNullOrEmpty(exclude))
            {
                regions = page.Regions;
            }
            else
            {
                string[] excludedNames = exclude.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                regions = page.Regions.Where(r => !excludedNames.Any(n => n.Equals(r.Name, StringComparison.InvariantCultureIgnoreCase)));
            }

            StringBuilder resultBuilder = new StringBuilder();
            foreach (RegionModel region in regions)
            {
                resultBuilder.Append(htmlHelper.DxaRegion(region, containerSize));
            }

            return new MvcHtmlString(resultBuilder.ToString());
        }

        #endregion

        #region Semantic markup extension methods

        /// <summary>
        /// Generates XPM markup for the current Page Model.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance on which the extension method operates.</param>
        /// <param name="isIncludePage">Specifies whether markup for an include Page (XPM edit include page/back button) should be rendered.</param>
        /// <returns>The XPM markup for the Page.</returns>
        /// <remarks>This method will throw an exception if the current Model does not represent a Page.</remarks>
        public static MvcHtmlString DxaPageMarkup(this HtmlHelper htmlHelper, bool isIncludePage = false)
        {
            // TODO TSI-776: this method should output "semantic" attributes on the HTML element representing the Page like we do for DxaRegionMarkup, DxaEntityMarkup and DxaPropertyMarkup
            if (!WebRequestContext.Localization.IsStaging)
            {
                return MvcHtmlString.Empty;
            }

            PageModel page = (PageModel) htmlHelper.ViewData.Model;

            Log.Debug("Rendering XPM markup for Page [{0}] (isIncludePage: {1})", page, isIncludePage);

            if (isIncludePage)
            {
                 return htmlHelper.Partial("Partials/XpmButton", page);
            }

            if (!page.XpmMetadata.ContainsKey("CmsUrl"))
            {
                page.XpmMetadata.Add("CmsUrl", SiteConfiguration.GetConfig("core.cmsurl", WebRequestContext.Localization));
            }

            return new MvcHtmlString(Tridion.Markup.TridionMarkup.PageMarkup(page.XpmMetadata)); 
        }

        /// <summary>
        /// Generates semantic markup (HTML/RDFa attributes) for the current Region Model.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance on which the extension method operates.</param>
        /// <returns>The HTML/RDFa attributes for the Region. These should be included in an HTML start tag.</returns>
        /// <remarks>This method will throw an exception if the current Model does not represent a Region.</remarks>
        public static MvcHtmlString DxaRegionMarkup(this HtmlHelper htmlHelper)
        {
            RegionModel region = (RegionModel) htmlHelper.ViewData.Model;
            return htmlHelper.DxaRegionMarkup(region);
        }

        /// <summary>
        /// Generates semantic markup (HTML/RDFa attributes) for a given Region Model.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance on which the extension method operates.</param>
        /// <param name="region">The Region Model to generate semantic markup for.</param>
        /// <returns>The HTML/RDFa attributes for the Region. These should be included in an HTML start tag.</returns>
        public static MvcHtmlString DxaRegionMarkup(this HtmlHelper htmlHelper, RegionModel region)
        {
            return Markup.RenderRegionAttributes(region);
        }

        /// <summary>
        /// Generates semantic markup (HTML/RDFa attributes) for the current Entity Model.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance on which the extension method operates.</param>
        /// <returns>The HTML/RDFa attributes for the Entity. These should be included in an HTML start tag.</returns>
        /// <remarks>This method will throw an exception if the current Model does not represent an Entity.</remarks>
        public static MvcHtmlString DxaEntityMarkup(this HtmlHelper htmlHelper)
        {
            EntityModel entity = (EntityModel) htmlHelper.ViewData.Model;
            return htmlHelper.DxaEntityMarkup(entity);
        }

        /// <summary>
        /// Generates semantic markup (HTML/RDFa attributes) for a given Entity Model.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance on which the extension method operates.</param>
        /// <param name="entity">The Entity Model to generate semantic markup for.</param>
        /// <returns>The HTML/RDFa attributes for the Entity. These should be included in an HTML start tag.</returns>
        public static MvcHtmlString DxaEntityMarkup(this HtmlHelper htmlHelper, EntityModel entity)
        {
            return Markup.RenderEntityAttributes(entity);
        }

        /// <summary>
        /// Generates semantic markup (HTML/RDFa attributes) for a given property of the current Entity Model.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance on which the extension method operates.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="index">The index of the property value (for multi-value properties).</param>
        /// <returns>The semantic markup (HTML/RDFa attributes).</returns>
        public static MvcHtmlString DxaPropertyMarkup(this HtmlHelper htmlHelper, string propertyName, int index = 0)
        {
            // TODO TSI-777: autogenerate index (?)
            EntityModel entity = (EntityModel) htmlHelper.ViewData.Model;
            return Markup.RenderPropertyAttributes(entity, propertyName, index);
        }

        /// <summary>
        /// Generates semantic markup (HTML/RDFa attributes) for a given property of a given Entity Model.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance on which the extension method operates.</param>
        /// <param name="entity">The Entity Model.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="index">The index of the property value (for multi-value properties).</param>
        /// <returns>The semantic markup (HTML/RDFa attributes).</returns>
        public static MvcHtmlString DxaPropertyMarkup(this HtmlHelper htmlHelper, EntityModel entity, string propertyName, int index = 0)
        {
            return Markup.RenderPropertyAttributes(entity, propertyName, index);
        }

        /// <summary>
        /// Generates semantic markup (HTML/RDFa attributes) for a given property.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance on which the extension method operates.</param>
        /// <param name="propertyExpression">A parameterless lambda expression which evaluates to a property of the current Entity Model.</param>
        /// <param name="index">The index of the property value (for multi-value properties).</param>
        /// <returns>The semantic markup (HTML/RDFa attributes).</returns>
        public static MvcHtmlString DxaPropertyMarkup(this HtmlHelper htmlHelper, Expression<Func<object>> propertyExpression, int index = 0)
        {
            // TODO TSI-777: autogenerate index (?)
            MemberExpression memberExpression = propertyExpression.Body as MemberExpression;
            if (memberExpression == null)
            {
                UnaryExpression boxingExpression = propertyExpression.Body as UnaryExpression;
                if (boxingExpression != null)
                {
                    memberExpression = boxingExpression.Operand as MemberExpression;
                }
            }
            if (memberExpression == null)
            {
                throw new DxaException(
                    string.Format("Unexpected expression provided to DxaPropertyMarkup: {0}. Expecting a lambda which evaluates to an Entity Model property.", propertyExpression.Body.GetType().Name)
                    );
            }

            Expression<Func<object>> entityExpression = Expression.Lambda<Func<object>>(memberExpression.Expression);
            Func<object> entityLambda = entityExpression.Compile();
            object entity = entityLambda.Invoke();
            EntityModel entityModel = entity as EntityModel;
            if (entityModel == null)
            {
                throw new DxaException(
                    string.Format("Unexpected type used in DxaPropertyMarkup expression: {0}. Expecting a lambda which evaluates to an Entity Model property.", entity)
                    );
            }

            return Markup.RenderPropertyAttributes(entityModel, memberExpression.Member, index);
        }
        #endregion

        // TODO: These are not HtmlHelper extension methods; move to another class.
        #region TODO
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
        #endregion
    }
}
