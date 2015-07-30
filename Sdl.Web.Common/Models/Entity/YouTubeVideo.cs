using System;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Common.Models
{
    [SemanticEntity(SchemaOrgVocabulary, "VideoObject", Prefix = "s", Public = true)]
    public class YouTubeVideo : MediaItem
    {
        public string Headline { get; set; }
        public string YouTubeId { get; set; }
        // TODO determine correct width and height or allow to be set
        public int Width { get { return 640; } }
        public int Height { get { return 390; } }

        /// <summary>
        /// Renders an HTML representation of the Media Item.
        /// </summary>
        /// <param name="widthFactor">The factor to apply to the width - can be % (eg "100%") or absolute (eg "120").</param>
        /// <param name="aspect">The aspect ratio to apply.</param>
        /// <param name="cssClass">Optional CSS class name(s) to apply.</param>
        /// <param name="containerSize">The size (in grid column units) of the containing element.</param>
        /// <returns>The HTML representation.</returns>
        /// <remarks>
        /// This method is used by the <see cref="IRichTextFragment.ToHtml()"/> implementation and by the HtmlHelperExtensions.Media implementation.
        /// Both cases should be avoided, since HTML rendering should be done in View code rather than in Model code.
        /// </remarks>
        public override string ToHtml(string widthFactor, double aspect = 0, string cssClass = null, int containerSize = 0)
        {
            if (string.IsNullOrEmpty(Url))
            {
                return string.Format(
                        "<iframe src=\"https://www.youtube.com/embed/{0}?version=3&enablejsapi=1\" id=\"video{1}\" class=\"{2}\"/>",
                        YouTubeId, Guid.NewGuid().ToString("N"), null
                        );
            }
            else
            {
                string htmlTagName = IsEmbedded ? "span" : "div";
                string placeholderImageUrl = string.IsNullOrEmpty(Url) ? null : SiteConfiguration.MediaHelper.GetResponsiveImageUrl(Url, aspect, widthFactor, containerSize);

                return string.Format(
                        "<{4} class=\"embed-video\"><img src=\"{1}\" alt=\"{2}\"><button type=\"button\" data-video=\"{0}\" class=\"{3}\"><i class=\"fa fa-play-circle\"></i></button></{4}>",
                        YouTubeId, placeholderImageUrl, Headline, (string) null, htmlTagName
                        );
            }
        }
    }
}