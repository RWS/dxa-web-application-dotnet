using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Represents a set of Region Models which can be indexed by name.
    /// </summary>
    [Serializable]
    public class RegionModelSet : HashSet<RegionModel>
    {
        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="RegionModelSet"/> instance for an empty set.
        /// </summary>
        public RegionModelSet()           
        {
        }

        /// <summary>
        /// Initializes a new <see cref="RegionModelSet"/> instance from a given set of Region Models.
        /// </summary>
        /// <param name="regionModels">The set of Region Models</param>
        public RegionModelSet(IEnumerable<RegionModel> regionModels)
            : base(regionModels)
        {
        }

        protected RegionModelSet(SerializationInfo info, StreamingContext context)
        {
            var list = (List<RegionModel>)info.GetValue("hashset", typeof(List<RegionModel>));
            foreach (RegionModel regionModel in list)
            {
                Add(regionModel);
            }
        }       

        #endregion

        /// <summary>
        /// Gets a Region by its name (indexer).
        /// </summary>
        /// <param name="name">The name of the Region.</param>
        /// <returns>A Region with given name.</returns>
        /// <exception cref="DxaException">If no Region exists with the given name.</exception>
        public RegionModel this[string name]
        {
            get
            {
                RegionModel region;
                if (!TryGetValue(name, out region))
                {
                    throw new DxaException($"Region '{name}' not found.");
                }
                return region;
            }
        }

        /// <summary>
        /// Tries to get a Region by its name.
        /// </summary>
        /// <param name="name">The name of the Region.</param>
        /// <param name="region">The Region or <c>null</c> if no Region exists with the given name.</param>
        /// <returns><c>true</c> if a Region exists with the given name.</returns>
        /// <remarks>This method has the same signature as <c>IDictionary&lt;string, RegionModel&gt;</c> for compatibility purposes.</remarks>
        public bool TryGetValue(string name, out RegionModel region)
        {
            region = this.FirstOrDefault(r => r.Name == name);
            return (region != null);
        }

        /// <summary>
        /// Determines whether a Region with a given name exists.
        /// </summary>
        /// <param name="name">The name of the Region.</param>
        /// <returns><c>true</c> if a Region with the given name exists.</returns>
        /// <remarks>This method has the same signature as <c>IDictionary&lt;string, RegionModel&gt;</c> for compatibility purposes.</remarks>
        public bool ContainsKey(string name)
        {
            return this.Any(r => r.Name == name);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("hashset", this.ToList<RegionModel>());
        }
    }
}
