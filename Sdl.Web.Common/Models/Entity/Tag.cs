using System;
namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Represents a Keyword in CM.
    /// </summary>
    /// <remarks>
    /// This class has hard-coded mappings to Keyword properties and does not support custom metadata on Keywords.
    /// If this is too limiting for your implemetation, use class <see cref="KeywordModel"/> instead.
    /// Since there is no use in subclassing this class (unlike <see cref="KeywordModel"/>), it has been declared as <c>sealed</c> in DXA 1.7.
    /// </remarks>
    /// <seealso cref="KeywordModel"/>
    [Serializable]
    public sealed class Tag
    {
        /// <summary>
        /// Gets or sets the display text.
        /// </summary>
        /// <remarks>
        /// This corresponds to the CM Keyword's Description or Title (if the CM Keyword has no Description).
        /// </remarks>
        public string DisplayText { get; set; }

        /// <summary>
        /// Gets or set a unique identifier for the tag (within the given domain/category/taxonomy).
        /// </summary>
        /// <remarks>
        /// This corresponds to the CM Keyword's Key or Id (if the CM Keyword has no Key).
        /// </remarks>
        public string Key { get; set; }
        
        /// <summary>
        /// The domain/category/taxonomy identifier for the tag.
        /// </summary>
        public string TagCategory { get; set; }
    }
}
