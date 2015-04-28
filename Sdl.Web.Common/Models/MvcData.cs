using System.Collections.Generic;
using System.Web.UI.WebControls;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Represents data about the Model, View and Controller
    /// </summary>
    public class MvcData
    {
        public string ControllerName { get; set; }
        public string ControllerAreaName { get; set; }
        public string ActionName { get; set; }
        public string ViewName { get; set; }
        public string AreaName { get; set; }
        public string RegionName { get; set; }
        public string RegionAreaName { get; set; }
        public Dictionary<string, string> RouteValues { get; set; }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            MvcData other = obj as MvcData;
            if (other == null)
                return false;

            // TODO: what about RouteValues?
            return (ControllerName == other.ControllerName) &&
                (ControllerAreaName == other.ControllerAreaName) &&
                (ActionName == other.ActionName) &&
                (ViewName == other.ViewName) &&
                (AreaName == other.AreaName) &&
                (RegionName == other.RegionName) &&
                (RegionAreaName == other.RegionAreaName);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        public override int GetHashCode()
        {
            return unchecked (
                SafeHashCode(ControllerName) ^
                SafeHashCode(ControllerAreaName) ^
                SafeHashCode(ActionName) ^
                SafeHashCode(ViewName) ^
                SafeHashCode(AreaName) ^
                SafeHashCode(RegionName) ^
                SafeHashCode(RegionAreaName)
                );
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            // TODO: what about the other properties?
            return string.Format("{0}:{1}:{2}", AreaName, ControllerName, ViewName);
        }

        private static int SafeHashCode(object obj)
        {
            return (obj == null) ? 0 : obj.GetHashCode();
        }
    }
}
