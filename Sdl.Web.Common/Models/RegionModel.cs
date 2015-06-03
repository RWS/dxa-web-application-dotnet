using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Represents the View Model for a Page Region.
    /// </summary>
#pragma warning disable 618
    // TODO DXA 2.0: Ideally this would inherit directly from ViewModel, but for backward compatibility we need the legacy type inbetween.
    public class RegionModel : Region
#pragma warning restore 618
    {
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
        /// <param name="viewName">The name of the View to use to render the Region.</param>
        public RegionModel(string name, string viewName) 
            : base(name)
        {
            MvcData = new MvcData
            {
                ViewName = viewName
            };
        }
        #endregion

        #region Overrides
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
    }
}