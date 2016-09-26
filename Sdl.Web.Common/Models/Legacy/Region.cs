using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Represents the View Model for a Page Region.
    /// </summary>
    [Obsolete("Deprecated in DXA 1.1. Use class RegionModel instead.")]
#pragma warning disable 618
    public abstract class Region : ViewModel, IRegion
#pragma warning restore 618
    {
        private readonly string _name;
        protected IList<EntityModel> _entities = new List<EntityModel>();

        /// <summary>
        /// Gets or sets the name of the Region.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                throw new NotSupportedException("Setting this property is not supported in DXA 1.1");
            }
        }

        /// <summary>
        /// Items are the raw entities that make up the page (eg Component Presentations, or other regions).
        /// </summary>
        [JsonIgnore]
        [Obsolete("Deprecated in DXA 1.1. Use property Entities instead.")]
        public List<object> Items
        {
            get
            {
                return _entities.Cast<object>().ToList();
            }
            set
            {
                throw new NotSupportedException("Setting this property is not supported in DXA 1.1. Use property Entities instead.");
            }
        }

        [JsonIgnore]
        [Obsolete("Deprecated in DXA 1.1. Use property XpmMetadata instead.")]
        public Dictionary<string, string> RegionData
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

        [JsonIgnore]
        [Obsolete("Deprecated in DXA 1.1. Use MvcData.AreaName instead.", true)]
        public string Module
        {
            get
            {
                return (MvcData == null) ? null : MvcData.AreaName;
            }
            set
            {
                throw new NotSupportedException("Setting this property is not supported in DXA 1.1. Use property MvcData instead.");
            }
        }

        #region Constructors
        /// <summary>
        /// Initializes a new Region instance.
        /// </summary>
        [Obsolete("Deprecated in DXA 1.1. Use class RegionModel instead.")]
        public Region()
        {
        }

        /// <summary>
        /// Initializes a new Region instance with a given name
        /// </summary>
        /// <param name="name">The name of the Region.</param>
        protected Region(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new DxaException("Region must have a non-empty name.");
            }
            _name = name;
        }

        #endregion

        #region IClonable members
        /// <summary>
        /// Creates a deep copy of this View Model.
        /// </summary>
        /// <returns>The copied View Model.</returns>
        public override object Clone()
        {
            Region clone = (Region) base.Clone();
            clone._entities = _entities.Select(e => (EntityModel) e.Clone()).ToList();
            return clone;
        }
        #endregion
    }
}
