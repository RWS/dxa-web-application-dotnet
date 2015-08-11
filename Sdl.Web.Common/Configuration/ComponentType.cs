namespace Sdl.Web.Common.Configuration
{
    /// <summary>
    /// Represents an (XPM) Component Type as configured in regions.json.
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
    }
}
