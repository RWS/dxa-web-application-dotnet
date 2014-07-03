using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Tridion
{
    /// <summary>
    /// Class for deserialized json component type.
    /// {"Schema":"tcm:4-208-8","Template":"tcm:4-206-32"}
    /// </summary>
    public class ComponentType
    {
        /// <summary>
        /// Schema URI.
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// Component Template URI.
        /// </summary>
        public string Template { get; set; }

        /// <summary>
        /// Initializes a new empty instance of the <see cref="ComponentType"/> class.
        /// </summary>
        public ComponentType() { }
    }
}
