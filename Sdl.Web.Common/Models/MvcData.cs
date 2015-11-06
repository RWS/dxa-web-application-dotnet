using System.Collections.Generic;
using System.Linq;
using Sdl.Web.Common.Configuration;

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
        public string RegionAreaName { get; set; } // TODO: Do we really need this?
        public Dictionary<string, string> RouteValues { get; set; }

        #region Constructors
        /// <summary>
        /// Initializes a new empty <see cref="MvcData"/> instance.
        /// </summary>
        public MvcData()
        {
        }

        /// <summary>
        /// Initializes a new <see cref="MvcData"/> instance for a given qualified View name.
        /// </summary>
        /// <param name="qualifiedViewName">The qualified View name with format AreaName:ControllerName:ViewName.</param>
        public MvcData(string qualifiedViewName)
        {
            string[] qualifiedViewNameParts = qualifiedViewName.Split(':');
            switch (qualifiedViewNameParts.Length)
            {
                case 1:
                    AreaName = SiteConfiguration.GetDefaultModuleName();
                    ViewName = qualifiedViewNameParts[0];
                    break;
                case 2:
                    AreaName = qualifiedViewNameParts[0];
                    ViewName = qualifiedViewNameParts[1];
                    break;
                case 3:
                    AreaName = qualifiedViewNameParts[0];
                    ControllerName = qualifiedViewNameParts[1];
                    ViewName = qualifiedViewNameParts[2];
                    break;
                default:
                    throw new DxaException(
                        string.Format("Invalid format for Qualified View Name: '{0}'. Format must be 'ViewName' or 'AreaName:ViewName' or 'AreaName:ControllerName:Vieweame.'", 
                            qualifiedViewName)
                            );
            }
        }

        /// <summary>
        /// Initializes a new <see cref="MvcData"/> instance which is a copy of another.
        /// </summary>
        /// <param name="other">The other <see cref="MvcData"/> to copy.</param>
        public MvcData(MvcData other)
        {
            ControllerName = other.ControllerName;
            ControllerAreaName = other.ControllerAreaName;
            ActionName = other.ActionName;
            ViewName = other.ViewName;
            AreaName = other.AreaName;
            RegionName = other.RegionName;
            RegionAreaName = other.RegionAreaName;
            if (other.RouteValues != null)
            {
                RouteValues = new Dictionary<string, string>(other.RouteValues);
            }
        }
        #endregion

        #region Overrides
        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            MvcData other = obj as MvcData;
            if (other == null)
            {
                return false;
            }

            // NOTE: RegionName and RegionAreaName are not included in equality check (these merely carry some additional info).
            if ((ControllerName != other.ControllerName) ||
                (ControllerAreaName != other.ControllerAreaName) ||
                (ActionName != other.ActionName) ||
                (ViewName != other.ViewName) ||
                (AreaName != other.AreaName))
            {
                return false;
            }

            if (RouteValues == null)
            {
                return other.RouteValues == null;
            }
            return RouteValues.SequenceEqual(other.RouteValues);
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
                SafeHashCode(AreaName)
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
        #endregion

        private static int SafeHashCode(object obj)
        {
            return (obj == null) ? 0 : obj.GetHashCode();
        }
    }
}
