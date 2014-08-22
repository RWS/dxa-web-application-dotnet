namespace Sdl.Web.Common.Models
{
    public class Tag
    {
        /// <summary>
        /// Text to display.
        /// </summary>
        public string DisplayText { get; set; }
        
        /// <summary>
        /// Unique identifier for the tag (within the given domain).
        /// </summary>
        public string Key { get; set; }
        
        /// <summary>
        /// The domain/category/taxonomy identifier for the tag.
        /// </summary>
        public string TagCategory { get; set; }
    }
}
