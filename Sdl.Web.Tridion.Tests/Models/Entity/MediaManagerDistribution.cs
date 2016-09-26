using System;
using System.Globalization;
using System.Xml;
using Newtonsoft.Json;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Tests.Models
{
    [SemanticEntity(CoreVocabulary, "ExternalContentLibraryStubSchemamm")]
    public class MediaManagerDistribution : EclItem
    {
        public string PlayerType { get; set; }
        public string CustomVideoAutoplay { get; set; }
        public string CustomVideoSubtitles { get; set; }
        public string CustomVideoControls { get; set; }
      
        /// <summary>
        /// Media Manager distribution GUID
        /// </summary>
        public string GlobalId
        {
            get
            {
                // Try to get GlobalId from ECL External Metadata
                object globalId;
                if (EclExternalMetadata != null && EclExternalMetadata.TryGetValue("GlobalId", out globalId))
                {
                    return globalId as string;
                }

                // Fallback: get the Global ID out of the direct link
                return Url.Substring(Url.ToLowerInvariant().LastIndexOf("?o=", StringComparison.Ordinal) + 3);
            }
        }

        /// <summary>
        /// URL that can be used to obtain Distribution JSON.
        /// </summary>
        public string DistributionJsonUrl
        {
            get
            {
                Uri directLinkUrl = new Uri(Url, UriKind.Absolute);
                return string.Format("{0}/json/{1}", directLinkUrl.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped), GlobalId);
            }
        }

        /// <summary>
        /// Media Manager distribution embed script URL
        /// </summary>
        public string EmbedScriptUrl
        {
            get
            {
                // transform mm direct link into embed script url
                // MM script url  https://mmecl.dist.sdlmedia.com/distributions/embed/?o=3E5F81F2-C7B3-47F7-8EDE-B84B447195B9
                // MM direct link https://mmecl.dist.sdlmedia.com/distributions/?o=3E5F81F2-C7B3-47F7-8EDE-B84B447195B9
                return Url.ToLowerInvariant().Replace("distributions/?o=", "distributions/embed/?o=");
            }
        }

        public override string GetIconClass()
        {
            // Try to get the MM Asset's MIME Type from ECL External Metadata
            string assetMimeType = GetEclExternalMetadataValue("Program/Asset/MIMEType") as string;
            if (assetMimeType == null)
            {
                return base.GetIconClass();
            }

            string fileType;
            return FontAwesomeMimeTypeToIconClassMapping.TryGetValue(assetMimeType, out fileType) ? string.Format("fa-file-{0}-o", fileType) : "fa-file";
        }     

        /// <summary>
        /// Returns true if the custom player is enabled
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        [JsonIgnore]
        public bool IsCustomPlayerEnabled
        {
            get
            {
                return PlayerType != null && PlayerType.Equals("Custom", StringComparison.InvariantCultureIgnoreCase);
            }
        }

        /// <summary>
        /// Returns true if autoplay is enabled
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        [JsonIgnore]
        public bool IsCustomVideoAutoplay
        {
            get
            {
                return CustomVideoAutoplay != null && CustomVideoAutoplay.Equals("Enabled", StringComparison.InvariantCultureIgnoreCase);
            }
        }

        /// <summary>
        /// Returns true if subtitles should be shown
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        [JsonIgnore]
        public bool IsCustomVideoSubtitles
        {
            get
            {
                return CustomVideoSubtitles != null && CustomVideoSubtitles.Equals("Enabled", StringComparison.InvariantCultureIgnoreCase);
            }
        }

        /// <summary>
        /// Returns true if the video controls should be shown
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        [JsonIgnore]
        public bool IsCustomVideoControls
        {
            get
            {
                return CustomVideoControls != null && CustomVideoControls.Equals("Enabled", StringComparison.InvariantCultureIgnoreCase);
            }
        }

        /// <summary>
        /// Gets the default View.
        /// </summary>
        /// <param name="localization">The context Localization</param>
        /// <remarks>
        /// This makes it possible possible to render "embedded" MediaManagerDistribution Models using the Html.DxaEntity method.
        /// </remarks>
        public override MvcData GetDefaultView(Localization localization)
        {
            return new MvcData("MediaManager:" + EclDisplayTypeId);
        }

        /// <summary>
        /// Renders an HTML representation of the Media Manager Distribution.
        /// </summary>
        /// <param name="widthFactor">The factor to apply to the width - can be % (eg "100%") or absolute (eg "120").</param>
        /// <param name="aspect">The aspect ratio to apply.</param>
        /// <param name="cssClass">Optional CSS class name(s) to apply.</param>
        /// <param name="containerSize">The size (in grid column units) of the containing element.</param>
        /// <returns>The HTML representation.</returns>
        /// <remarks>
        /// This method is used by the <see cref="IRichTextFragment.ToHtml()"/> implementation and by the HtmlHelperExtensions.Media implementation.
        /// </remarks>
        public override string ToHtml(string widthFactor, double aspect = 0, string cssClass = null, int containerSize = 0)
        {
            string classAttr = string.IsNullOrEmpty(cssClass) ? string.Empty : string.Format(" class=\"{0}\"", cssClass);

            switch (EclDisplayTypeId)
            {
                case "html5dist":
                    // The ECL Template Fragment for MM Video Distribution does not support responsive resizing (yet).
                    return string.Format("<div{0}><div id=\"{1}\"></div><script src=\"{2}&trgt={1}&responsive=true\"></script></div>", classAttr, Guid.NewGuid(), EmbedScriptUrl);

                case "imagedist":
                    // The ECL Template Fragment for MM Image Distribution doesn't allow us to control image sizing, so we create our own img tag here.
                    string widthAttr = string.IsNullOrEmpty(widthFactor) ? string.Empty : string.Format(" width=\"{0}\"", widthFactor);
                    string aspectAttr = (aspect == 0) ? string.Empty : string.Format(" data-aspect=\"{0}\"", aspect.ToString(CultureInfo.InvariantCulture));
                    return string.Format("<img src=\"{0}\"{1}{2}{3}>", Url, widthAttr, aspectAttr, classAttr);

                default:
                    // Let EclItem.ToHtml render the HTML based on the ECL Template Fragment.
                    return base.ToHtml(widthFactor, aspect, cssClass, containerSize);
            }
        }

        /// <summary>
        /// Read properties from XHTML element.
        /// </summary>
        /// <param name="xhtmlElement">XHTML element</param>
        public override void ReadFromXhtmlElement(XmlElement xhtmlElement)
        {
            base.ReadFromXhtmlElement(xhtmlElement);
            PlayerType = GetOptionalAttribute(xhtmlElement, "data-playerType");
            CustomVideoAutoplay = GetOptionalAttribute(xhtmlElement, "data-customVideoAutoplay");
            CustomVideoSubtitles = GetOptionalAttribute(xhtmlElement, "data-customVideoSubtitles");
            CustomVideoControls = GetOptionalAttribute(xhtmlElement, "data-customVideoControls");
        }

        private static string GetOptionalAttribute(XmlElement xmlElement, string name)
        {
            XmlAttribute attribute = xmlElement.GetAttributeNode(name);
            return (attribute == null) ? null : attribute.Value;
        }
    }
}
