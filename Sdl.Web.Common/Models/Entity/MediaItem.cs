using System;
using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    [SemanticEntity(Vocab = SchemaOrgVocabulary, EntityName = "MediaObject", Prefix = "s", Public = true)]
    public abstract class MediaItem : EntityModel
    {
        static readonly IDictionary<string, string> _fontAwesomeMimeTypeToIconClassMapping = new Dictionary<string, string>
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

        [SemanticProperty("s:contentUrl")]
        public string Url { get; set; }
        public string FileName { get; set; }
        [SemanticProperty("s:contentSize")]
        public int FileSize { get; set; }
        public string MimeType { get; set; }

        /// <summary>
        /// Gets the file size with units.
        /// </summary>
        public string GetFriendlyFileSize()
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            double len = FileSize;
            int order = 0;
            while (len >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                len = len / 1024;
            }

            return string.Format("{0} {1}", Math.Ceiling(len), sizes[order]);
        }

        /// <summary>
        /// Gets the name of a CSS class representing the Icon for this Media Item.
        /// </summary>
        /// <returns>The CSS class name.</returns>
        public virtual string GetIconClass()
        {
            string fileType;
            return _fontAwesomeMimeTypeToIconClassMapping.TryGetValue(MimeType, out fileType) ? string.Format("fa-file-{0}-o", fileType) : "fa-file";
        }

        /// <summary>
        /// Renders an HTML representation of the Entity Model.
        /// </summary>
        /// <returns>An HTML representation.</returns>
        /// <remarks>
        /// This method is used when the Entity Model is part of a <see cref="RichText"/> instance which is mapped to a string property.
        /// In this case HTML rendering happens during model mapping, which is not ideal.
        /// Preferably, the model property should be of type <see cref="RichText"/> and the View should use @Html.DxaRichText() to get the rich text rendered as HTML.
        /// </remarks>
        public override string ToHtml()
        {
            return ToHtml("100%");
        }

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
        public abstract string ToHtml(string widthFactor, double aspect = 0, string cssClass = null, int containerSize = 0);
    }
}