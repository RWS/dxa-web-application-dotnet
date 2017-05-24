using System.Collections.Generic;
using System.ServiceModel.Syndication;
using Newtonsoft.Json;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using System;
using System.Linq;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Represents the View Model for a Page
    /// </summary>
    [Serializable]
    public class PageModel : ViewModel, ISyndicationFeedItemProvider
    {
        private const string XpmPageSettingsMarkup = "<!-- Page Settings: {{\"PageID\":\"{0}\",\"PageModified\":\"{1}\",\"PageTemplateID\":\"{2}\",\"PageTemplateModified\":\"{3}\"}} -->";
        private const string XpmPageScript = "<script type=\"text/javascript\" language=\"javascript\" defer=\"defer\" src=\"{0}/WebUI/Editors/SiteEdit/Views/Bootstrap/Bootstrap.aspx?mode=js\" id=\"tridion.siteedit\"></script>";

        /// <summary>
        /// Gets the Page Regions.
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public RegionModelSet Regions { get; private set; } = new RegionModelSet();

        /// <summary>
        /// Specifies whether the Page Model can be cached or not.
        /// </summary>
        [JsonIgnore]
        [SemanticProperty(IgnoreMapping = true)]
        public bool NoCache { get; set; }

        /// <summary>
        /// Gets or sets the URL path of the Page.
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public string Url { get;  set; }

        /// <summary>
        /// Gets or sets the Page metadata which is typically rendered as HTML meta tags (name/value pairs).
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public IDictionary<string, string> Meta { get; set; }

        /// <summary>
        /// Gets or sets the identifier for the Page.
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public string Id { get; private set; }

        /// <summary>
        /// Gets or sets the Title of the Page which is typically rendered as HTML title tag.
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public string Title { get; set; }

        public PageModel()
        {
            // required for deserialization
        }

        /// <summary>
        /// Initializes a new instance of PageModel.
        /// </summary>
        /// <param name="id">The identifier of the Page.</param>
        public PageModel(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new DxaException("Page Model must have a non-empty identifier.");
            }
            Id = id;

            Meta = new Dictionary<string, string>();
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
                XpmPageSettingsMarkup,
                XpmMetadata["PageID"],
                XpmMetadata["PageModified"],
                XpmMetadata["PageTemplateID"],
                XpmMetadata["PageTemplateModified"]
                ) +
                string.Format(XpmPageScript, cmsUrl);
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
        /// <param name="localization">The context Localization.</param>
        public void FilterConditionalEntities(Localization localization)
        {
            using (new Tracer(localization, this))
            {
                foreach (RegionModel region in Regions)
                {
                    region.FilterConditionalEntities(localization);
                }
            }
        }

        /// <summary>
        /// Creates a deep copy of this View Model.
        /// </summary>
        /// <returns>The copied View Model.</returns>
        public override ViewModel DeepCopy()
        {
            PageModel clone = (PageModel) base.DeepCopy();
            clone.Regions = new RegionModelSet(Regions.Select(r => (RegionModel) r.DeepCopy()));
            if (Meta != null)
            {
                clone.Meta = new Dictionary<string, string>(Meta);
            }
            return clone;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current Page Model.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the specified object in an Page Model with the same <see cref="Id"/> as the current one.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            PageModel other = obj as PageModel;
            if (other == null)
            {
                return false;
            }
            return other.Id == Id;
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current Page Model.
        /// </returns>
        public override int GetHashCode()
            => Id.GetHashCode();

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string containing the type, identifier and title of the Page.
        /// </returns>
        public override string ToString()
            => $"{GetType().Name}: {Id} ('{Title}')";
    }

}
