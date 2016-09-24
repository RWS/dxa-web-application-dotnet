using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using Sdl.Web.Common.Configuration;
using System.Text.RegularExpressions;
using System;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Represents the View Model for a Page Region.
    /// </summary>
#pragma warning disable 618
    // TODO DXA 2.0: Ideally this would inherit directly from ViewModel, but for backward compatibility we need the legacy type inbetween.
    public class RegionModel : Region, ISyndicationFeedItemProvider
#pragma warning restore 618
    {
        private const string XpmRegionMarkup = "<!-- Start Region: {{title: \"{0}\", allowedComponentTypes: [{1}], minOccurs: {2}}} -->";
        private const string XpmComponentTypeMarkup = "{{schema: \"{0}\", template: \"{1}\"}}";

        private readonly RegionModelSet _regions = new RegionModelSet();

        /// <summary>
        /// The XPM metadata key used for the ID of the (Include) Page from which the Region originates. Avoid using this in implementation code because it may change in a future release.
        /// </summary>
        public const string IncludedFromPageIdXpmMetadataKey = "IncludedFromPageID";

        /// <summary>
        /// The XPM metadata key used for the title of the (Include) Page from which the Region originates. Avoid using this in implementation code because it may change in a future release.
        /// </summary>
        public const string IncludedFromPageTitleXpmMetadataKey = "IncludedFromPageTitle";

        /// <summary>
        /// The XPM metadata key used for the file name of the (Include) Page from which the Region originates. Avoid using this in implementation code because it may change in a future release.
        /// </summary>
        public const string IncludedFromPageFileNameXpmMetadataKey = "IncludedFromPageFileName";

        /// <summary>
        /// Gets the Entities that the Region contains.
        /// </summary>
        public IList<EntityModel> Entities
        {
            get
            {
                return _entities;
            }
        }

        /// <summary>
        /// Gets the (nested) Regions within this Region.
        /// </summary>
        public RegionModelSet Regions
        {
            get
            {
                return _regions;
            }
        }

        public String NameWithoutPostfix
        {
            get
            {
                var match = Regex.Match(Name, @"(.*)___\d+");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                return Name;
            }
        }
        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="RegionModel"/> instance.
        /// </summary>
        /// <param name="name">The name of the Region.</param>
        public RegionModel(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="RegionModel"/> instance for an empty/non-existing Region.
        /// </summary>
        /// <param name="name">The name of the Region.</param>
        /// <param name="qualifiedViewName">The qualified name of the View to use to render the Region. Format: format AreaName:ControllerName:ViewName.</param>
        public RegionModel(string name, string qualifiedViewName) 
            : base(name)
        {
            MvcData = new MvcData(qualifiedViewName)
            {
                ActionName = "Region"
            };
        }
        #endregion

        public String GetNameWithPostfix(int counter)
        {
            return String.Format("{0}___{1}", this.Name, counter);
        }

        #region Overrides

        /// <summary>
        /// Gets the rendered XPM markup
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The XPM markup.</returns>
        public override string GetXpmMarkup(Localization localization)
        {
            XpmRegion xpmRegion =  localization.GetXpmRegionConfiguration(this.NameWithoutPostfix);
            if (xpmRegion == null)
            {
                return string.Empty;
            }

            // TODO: obtain MinOccurs & MaxOccurs from regions.json
            return string.Format(
                XpmRegionMarkup, 
                Name, 
                string.Join(", ", xpmRegion.ComponentTypes.Select(ct => string.Format(XpmComponentTypeMarkup, ct.Schema, ct.Template))), 
                0);

        }

        /// <summary>
        /// Determines whether the specified object is equal to the current Region Model, i.e. it has the same name.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the specified object is a <see cref="RegionModel"/> with the same name as this one.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            RegionModel other = obj as RegionModel;
            if (other == null)
            {
                return false;
            }
            return other.Name == Name;
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current Region Model (based on its name).
        /// </returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string containing the type and name of the Region.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0} '{1}'", GetType().Name, Name);
        }
        #endregion

        #region ISyndicationFeedItemProvider members
        /// <summary>
        /// Extracts syndication feed items.
        /// </summary>
        /// <param name="localization">The context <see cref="Localization"/>.</param>
        /// <returns>The extracted syndication feed items; a concatentation of syndication feed items provided by <see cref="Entities"/> (if any).</returns>
        public virtual IEnumerable<SyndicationItem> ExtractSyndicationFeedItems(Localization localization)
        {
            return ConcatenateSyndicationFeedItems(Entities.OfType<ISyndicationFeedItemProvider>(), localization);
        }
        #endregion
    }
}