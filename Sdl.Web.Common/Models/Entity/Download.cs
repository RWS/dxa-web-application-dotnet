using System;
using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    [SemanticEntity(SchemaOrgVocabulary, "DataDownload", Prefix = "s", Public = true)]
    public class Download : MediaItem
    {
        [SemanticProperty("s:name")]
        [SemanticProperty("s:description")]
        public string Description { get; set; }

        /// <summary>
        /// Renders an HTML representation of the Media Item.
        /// </summary>
        /// <param name="widthFactor">The factor to apply to the width - can be % (eg "100%") or absolute (eg "120").</param>
        /// <param name="aspect">The aspect ratio to apply.</param>
        /// <param name="cssClass">Optional CSS class name(s) to apply.</param>
        /// <param name="containerSize">The size (in grid column units) of the containing element.</param>
        /// <returns>The HTML representation.</returns>
        /// <remarks>
        /// This method is used by the <see cref="IRichTextFragment.ToHtml()"/> implementation in <see cref="MediaItem"/> and by the HtmlHelperExtensions.Media implementation.
        /// Both cases should be avoided, since HTML rendering should be done in View code rather than in Model code.
        /// </remarks>
        public override string ToHtml(string widthFactor, double aspect = 0, string cssClass = null, int containerSize = 0)
        {
            string descriptionHtml = string.IsNullOrEmpty(Description) ? null : string.Format("<small>{0}</small>", Description);
            return string.Format(@"
                <div class=""download-list"">
                    <i class=""fa {0}""></i>
                    <div>
                        <a href=""{1}"">{2}</a> <small class=""size"">({3})</small>
                        {4}
                    </div>
                </div>", 
                GetIconClass(), Url, FileName, GetFriendlyFileSize(), descriptionHtml);
        }
    }
}