namespace Sdl.Web.Common.Configuration
{
    /// <summary>
    /// Represents an (XPM) Occurrence Constraint as configured in regions.json.
    /// </summary>
    public class OccurrenceConstraint
    {
        /// <summary>
        /// The minimum number of Component Presentation(s) in the Region.
        /// </summary>
        public int MinOccurs { get; set; }

        /// <summary>
        /// The maximum number of Component Presentation(s) in the Region.
        /// </summary>
        public int MaxOccurs { get; set; }
    }
}
