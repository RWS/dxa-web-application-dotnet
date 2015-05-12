using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Abstract base class for all (strongly typed) View Models
    /// </summary>
    public abstract class ViewModel
    {
        /// <summary>
        /// The (internal) Vocabulary URI used for semantic mapping of some of the Core Entity Models.
        /// </summary>
        public const string CoreVocabulary = "http://www.sdl.com/web/schemas/core"; //TODO: make internal?

        /// <summary>
        /// Gets or sets MVC data used to determine which View to use.
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public MvcData MvcData
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets metadata used to render XPM markup
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public IDictionary<string, string> XpmMetadata
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes a new ViewModel instance.
        /// </summary>
        protected ViewModel()
        {
            XpmMetadata = new Dictionary<string, string>();
        }
    }
}
