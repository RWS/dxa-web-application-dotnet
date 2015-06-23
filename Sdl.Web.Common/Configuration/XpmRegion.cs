using System.Collections.Generic;

namespace Sdl.Web.Common.Configuration
{
    /// <summary>
    /// Represents the configuration for an XPM Region (as configured in regions.json)
    /// </summary>
    public class XpmRegion
    {
        /// <summary>
        /// Region name.
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Allowed Component Types.
        /// </summary>
        public List<ComponentType> ComponentTypes { get; set; }
    }
}
