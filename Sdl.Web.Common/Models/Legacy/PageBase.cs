using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Legacy base class for page content, used for includes and other 'non-concrete' pages. 
    /// </summary>
    [Obsolete("Deprecated in DXA 1.1. Use class PageModel instead.")]
    public abstract class PageBase : ViewModel, IPage
    {
        private readonly string _id;
        protected RegionModelSet _regions = new RegionModelSet();

        #region IPage members
        /// <summary>
        /// Gets or sets the identifier for the Page.
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public string Id
        {
            get
            {
                return _id;
            }
            set
            {
                throw new NotSupportedException("Setting this property is not supported in DXA 1.1. Use PageModel constructor to set Id.");
            }
        }

        /// <summary>
        /// Gets or sets the Title of the Page which is typically rendered as HTML title tag.
        /// </summary>
        public string Title
        {
            get;
            set;
        }

        [SemanticProperty(IgnoreMapping = true)]
        [JsonIgnore]
        [Obsolete("Deprecated in DXA 1.1. Use PageModel.Regions instead.")]
        public Dictionary<string, IRegion> Regions
        {
            get
            {
                return _regions.Cast<IRegion>().ToDictionary(r => r.Name);
            }
            set
            {
                throw new NotSupportedException("Setting this property is not supported in DXA 1.1. Use property PageModel.Regions instead.");
            }
        }

        /// <summary>
        /// For storing system data (for example page id and modified date for xpm markup).
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        [JsonIgnore]
        [Obsolete("Deprecated in DXA 1.1. Use property XpmMetadata instead.")]
        public Dictionary<string, string> PageData
        {
            get
            {
                return XpmMetadata as Dictionary<string, string>;
            }
            set
            {
                throw new NotSupportedException("Setting this property is not supported in DXA 1.1. Use property XpmMetadata instead.");
            }
        }

        [SemanticProperty(IgnoreMapping = true)]
        [JsonIgnore]
        [Obsolete("Deprecated in DXA 1.1. Use property MvcData instead.")]
        public MvcData AppData
        {
            get
            {
                return MvcData;
            }
            set
            {
                throw new NotSupportedException("Setting this property is not supported in DXA 1.1. Use property MvcData instead.");
            }
        }
        #endregion

        /// <summary>
        /// Initializes a new instance of PageBase
        /// </summary>
        /// <param name="id">The identifier for the Page.</param>
        protected PageBase(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new DxaException("Page Model must have a non-empty identifier.");
            }
            _id = id;
        }

        #region Overrides

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
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string containing the type, identifier and title of the Page.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0}: {1} ('{2}')", GetType().Name, Id, Title);
        }

        #endregion

        #region IClonable members
        /// <summary>
        /// Creates a deep copy of this View Model.
        /// </summary>
        /// <returns>The copied View Model.</returns>
        public override object Clone()
        {
            PageBase clone = (PageBase) base.Clone();
            clone._regions = new RegionModelSet(_regions.Select(r => (RegionModel) r.Clone()));
            return clone;
        }
        #endregion

    }
}