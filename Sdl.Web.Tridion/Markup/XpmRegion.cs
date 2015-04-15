using System.Collections.Generic;

namespace Sdl.Web.Tridion.Markup
{
    /// <summary>
    /// Class for deserialized json region.
    /// {"Region":"Main","ComponentTypes":[{"Schema":"tcm:4-208-8","Template":"tcm:4-206-32"},{"Schema":"tcm:4-41-8","Template":"tcm:4-199-32"}]}
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

        /// <summary>
        /// Initializes a new empty instance of the <see cref="XpmRegion"/> class.
        /// </summary>
        public XpmRegion() { }
    }
}
