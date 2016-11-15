using System.Collections.Generic;
using System.ServiceModel.Syndication;
using Newtonsoft.Json;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using System;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Represents the View Model for a Page
    /// </summary>
#pragma warning disable 618
    // TODO DXA 2.0: Should inherit directly from ViewModel, but for now we need the legacy classes inbetween for compatibility.
    [Serializable]
    public class PageModel : WebPage, ISyndicationFeedItemProvider
#pragma warning restore 618
    {
        private const string _xpmPageSettingsMarkup = "<!-- Page Settings: {{\"PageID\":\"{0}\",\"PageModified\":\"{1}\",\"PageTemplateID\":\"{2}\",\"PageTemplateModified\":\"{3}\"}} -->";
        private const string _xpmPageScript = "<script type=\"text/javascript\" language=\"javascript\" defer=\"defer\" src=\"{0}/WebUI/Editors/SiteEdit/Views/Bootstrap/Bootstrap.aspx?mode=js\" id=\"tridion.siteedit\"></script>";

        /// <summary>
        /// Gets the Page Regions.
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public new RegionModelSet Regions
        {
            get
            {
                return _regions;
            }
        }

        /// <summary>
        /// Specifies whether the Page Model can be cached or not.
        /// </summary>
        [JsonIgnore]
        [SemanticProperty(IgnoreMapping = true)]
        public bool NoCache { get; set; }

        /// <summary>
        /// Initializes a new instance of PageModel.
        /// </summary>
        /// <param name="id">The identifier of the Page.</param>
        public PageModel(string id)
            : base(id)
        {
        }

        #region Overrides

        /// <summary>
        /// Gets the rendered XPM markup
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The XPM markup.</returns>
        public override string GetXpmMarkup(Localization localization)
        {
            if (XpmMetadata == null)
            {
                return string.Empty;
            }

            string cmsUrl;
            object cmsUrlValue;
            if (XpmMetadata.TryGetValue("CmsUrl", out cmsUrlValue))
            {
                cmsUrl = (string) cmsUrlValue;
            }
            else
            {
                cmsUrl = localization.GetConfigValue("core.cmsurl");
            }
            if (cmsUrl.EndsWith("/"))
            {
                // remove trailing slash from cmsUrl if present
                cmsUrl = cmsUrl.Remove(cmsUrl.Length - 1);
            }

            return string.Format(
                _xpmPageSettingsMarkup,
                XpmMetadata["PageID"],
                XpmMetadata["PageModified"],
                XpmMetadata["PageTemplateID"],
                XpmMetadata["PageTemplateModified"]
                ) + 
                string.Format(_xpmPageScript, cmsUrl);
        }

        #endregion  

        #region ISyndicationFeedItemProvider members
        /// <summary>
        /// Extracts syndication feed items.
        /// </summary>
        /// <param name="localization">The context <see cref="Localization"/>.</param>
        /// <returns>The extracted syndication feed items; a concatentation of syndication feed items provided by <see cref="Regions"/> (if any).</returns>
        public virtual IEnumerable<SyndicationItem> ExtractSyndicationFeedItems(Localization localization)
        {
            return ConcatenateSyndicationFeedItems(Regions, localization);
        }
        #endregion

        /// <summary>
        /// Filters (i.e. removes) conditional Entities which don't meet the conditions.
        /// </summary>
        public void FilterConditionalEntities()
        {
            using (new Tracer(this))
            {
                foreach (RegionModel region in Regions)
                {
                    region.FilterConditionalEntities();
                }
            }
        }
    }

}
