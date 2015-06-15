using System;
using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Abstract base class for View Models for Entities.
    /// </summary>
    public abstract class EntityModel : ViewModel
    {
        private string _id = string.Empty;

        /// <summary>
        /// Gets or sets the identifier for the Entity.
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
                // The identifier is effectively immutable, but having a setter makes it more convenient (and backward compatible).
                if (!string.IsNullOrEmpty(_id))
                {
                    throw new DxaException("Cannot change the identifier of an Entity.");
                }
                if (string.IsNullOrEmpty(value))
                {
                    throw new DxaException("An Entity must have a non-empty identifier.");
                }
                _id = value;
            }
        }

        /// <summary>
        /// Gets or sets metadata used to render XPM property markup.
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public IDictionary<string, string> XpmPropertyMetadata
        {
            get;
            set;
        }

        #region Overrides

        /// <summary>
        /// Determines whether the specified object is equal to the current Entity Model.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the specified object in an Entity Model with the same <see cref="Id"/> as the current one.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            EntityModel other = obj as EntityModel;
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
        /// A hash code for the current Entity Model.
        /// </returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string containing the type and identifier of the Entity.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0}: {1}", GetType().Name, Id);
        }

        #endregion
    }
}
