using System.Collections.Generic;
using System.ServiceModel.Syndication;
using Newtonsoft.Json;
using Sdl.Web.Common.Logging;
using System;
using System.Globalization;
using System.Linq;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Represents the View Model for a Page
    /// </summary>
    [Serializable]
    public class PageModel : ViewModel, ISyndicationFeedItemProvider
    {
        private const string XpmPageSettingsMarkup = "<!-- Page Settings: {{\"PageID\":\"{0}\",\"PageModified\":\"{1}\",\"PageTemplateID\":\"{2}\",\"PageTemplateModified\":\"{3}\", allowedComponentTypes: [{4}], {5}}} -->";
        private const string XpmPageScript = "<script type=\"text/javascript\" language=\"javascript\" defer=\"defer\" src=\"{0}/WebUI/Editors/SiteEdit/Views/Bootstrap/Bootstrap.aspx?mode=js\" id=\"tridion.siteedit\"></script>";
        private const string XpmDateTimeFormat = "yyyy-MM-ddTHH:mm:ss";
        private const string OccurenceConstraintMarkupUnlimited = "minOccurs: {0}";
        private const string OccurenceConstraintMarkup = "minOccurs: {0}, maxOccurs: {1}";
        private const string XpmComponentTypeMarkup = "{{schema: \"{0}\", template: \"{1}\"}}";
        private const string DefaultTypeMarkup = "{schema: \"*\", template: \"*\"}";
        private const string DefaultOccurrenceMarkup = "minOccurs: 0";

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
            if (String.IsNullOrEmpty(id))
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
        public override string GetXpmMarkup(ILocalization localization)
        {
            string occurrenceConstraint = DefaultOccurrenceMarkup;
            string typeConstraint = DefaultTypeMarkup;
            if (XpmMetadata.ContainsKey("PageSchemaID"))
            {
                string pageSchemaId = (string)XpmMetadata["PageSchemaID"];
                XpmRegion xpmRegion = localization.GetXpmRegionConfiguration(pageSchemaId);
                if (xpmRegion != null)
                {
                    int minOccurs = xpmRegion.OccurrenceConstraint?.MinOccurs ?? 0;
                    int maxOccurs = xpmRegion.OccurrenceConstraint?.MaxOccurs ?? -1;
                    occurrenceConstraint = maxOccurs == -1
                        ? string.Format(OccurenceConstraintMarkupUnlimited, minOccurs)
                        : string.Format(OccurenceConstraintMarkup, minOccurs, maxOccurs);
                    typeConstraint = string.Join(", ",
                        xpmRegion.ComponentTypes.Select(
                            ct => string.Format(XpmComponentTypeMarkup, ct.Schema, ct.Template)));
                }
            }
            if (XpmMetadata == null)
            {
                return String.Empty;
            }
            string cmsUrl = (localization.GetConfigValue("core.cmsurl") ?? String.Empty).TrimEnd('/');
            string result =  String.Format(
                XpmPageSettingsMarkup,
                XpmMetadata["PageID"],
                GetDateTimeStr(XpmMetadata["PageModified"]),
                XpmMetadata["PageTemplateID"],
                GetDateTimeStr(XpmMetadata["PageTemplateModified"]),
                typeConstraint,
                occurrenceConstraint)
                + String.Format(XpmPageScript, cmsUrl);
            return result;
        }

        private static string GetDateTimeStr(object datetime)
        {
            // legacy will pass a string here but R2 uses DateTime and so must be converted to the right
            // format
            var s = datetime as string;
            return s ?? ((DateTime) datetime).ToString(XpmDateTimeFormat, CultureInfo.InvariantCulture);
        }

        #endregion

        #region ISyndicationFeedItemProvider members
        /// <summary>
        /// Extracts syndication feed items.
        /// </summary>
        /// <param name="localization">The context <see cref="ILocalization"/>.</param>
        /// <returns>The extracted syndication feed items; a concatentation of syndication feed items provided by <see cref="Regions"/> (if any).</returns>
        public virtual IEnumerable<SyndicationItem> ExtractSyndicationFeedItems(ILocalization localization)
        {
            return ConcatenateSyndicationFeedItems(Regions, localization);
        }
        #endregion

        /// <summary>
        /// Filters (i.e. removes) conditional Entities which don't meet the conditions.
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        public void FilterConditionalEntities(ILocalization localization)
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
