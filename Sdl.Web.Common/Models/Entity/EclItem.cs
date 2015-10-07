using System;
using System.Collections.Generic;
using System.Xml;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Abstract base class for View Models representing ECL items.
    /// </summary>
    public abstract class EclItem : MediaItem
    {
        /// <summary>
        /// ECL URI.
        /// </summary>
        public string EclUri { get; set; }

        /// <summary>
        /// ECL Display Type ID.
        /// </summary>
        public string EclDisplayTypeId { get; set; }

        /// <summary>
        /// ECL Template Fragment.
        /// </summary>
        public string EclTemplateFragment { get; set; }

        /// <summary>
        /// ECL External Metadata.
        /// </summary>
        /// <value>
        /// Keys are the field names. Values can be simple types (string, double, DateTime), nested IDictionaries and enumerables of those types. 
        /// </value>
        public IDictionary<string, object> EclExternalMetadata { get; set; }

        /// <summary>
        /// Gets the rendered XPM markup
        /// </summary>
        /// <remarks>
        /// ECL items will use ECL URI rather than TCM URI in XPM markup
        /// </remarks>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The XPM markup.</returns>
        public override string GetXpmMarkup(Localization localization)
        {
            if (XpmMetadata != null && !string.IsNullOrEmpty(EclUri))
            {
                XpmMetadata["ComponentID"] = EclUri;
            }
            return base.GetXpmMarkup(localization);
        }

        /// <summary>
        /// Renders an HTML representation of the ECL Item.
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
            // NOTE: we're ignoring all parameters here.
            return EclTemplateFragment;
        }

        /// <summary>
        /// Read properties from XHTML element.
        /// </summary>
        /// <param name="xhtmlElement">XHTML element</param>
        public override void ReadFromXhtmlElement(XmlElement xhtmlElement)
        {
            base.ReadFromXhtmlElement(xhtmlElement);
            EclUri = xhtmlElement.GetAttribute("data-eclId");
            EclDisplayTypeId = xhtmlElement.GetAttribute("data-eclDisplayTypeId");
            EclTemplateFragment = xhtmlElement.GetAttribute("data-eclTemplateFragment");

            // Note that FileName and MimeType are already set in MediaItem.ReadFromXhtmlElement.
            // We overwrite those with the values provided by ECL (if any).
            string eclFileName = xhtmlElement.GetAttribute("data-eclFileName");
            if (!string.IsNullOrEmpty(eclFileName))
            {
                FileName = eclFileName;
            }
            string eclMimeType = xhtmlElement.GetAttribute("data-eclMimeType");
            if (!string.IsNullOrEmpty(eclMimeType))
            {
                MimeType = eclMimeType;
            }
        }
    }
}
