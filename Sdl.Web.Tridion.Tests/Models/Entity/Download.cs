using System.Collections.Generic;
using System.ServiceModel.Syndication;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;
using System;
using Sdl.Web.Common.Interfaces;

namespace Sdl.Web.Tridion.Tests.Models
{
    [SemanticEntity(SchemaOrgVocabulary, "DataDownload", Prefix = "s", Public = true)]
    [Serializable]
    public class Download : MediaItem, ISyndicationFeedItemProvider
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
            if (string.IsNullOrEmpty(Url))
                return string.Empty;

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

        /// <summary>
        /// Gets the default View.
        /// </summary>
        /// <param name="localization">The context Localization</param>
        /// <remarks>
        /// This makes it possible possible to render "embedded" Download Models using the Html.DxaEntity method.
        /// </remarks>
        public override MvcData GetDefaultView(ILocalization localization)
        {
            return new MvcData("Core:Download");
        }

        #region ISyndicationFeedItemProvider members
        /// <summary>
        /// Extracts syndication feed items.
        /// </summary>
        /// <param name="localization">The context <see cref="ILocalization"/>.</param>
        /// <returns>A single syndication feed item containing information extracted from this <see cref="Teaser"/>.</returns>
        public IEnumerable<SyndicationItem> ExtractSyndicationFeedItems(ILocalization localization)
        {
            Link downloadLink = new Link {Url = Url}; 
            return new[] { CreateSyndicationItem(FileName, Description, downloadLink, null, localization) };
        }
        #endregion
    }
}