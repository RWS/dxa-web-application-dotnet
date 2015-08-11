using System.Collections.Generic;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Abstract base class for all (strongly typed) View Models
    /// </summary>
    public abstract class ViewModel
    {
        /// <summary>
        /// The internal/built-in Vocabulary ID used for semantic/CM mapping.
        /// </summary>
        public const string CoreVocabulary = "http://www.sdl.com/web/schemas/core";

        /// <summary>
        /// The Vocabulary ID for types defined by schema.org.
        /// </summary>
        public const string SchemaOrgVocabulary = "http://schema.org/"; 

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
        /// Gets or sets HTML CSS classes for use in View top level HTML element.
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public string HtmlClasses
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
        /// Gets the rendered XPM markup
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The XPM markup.</returns>
        public abstract string GetXpmMarkup(Localization localization);
    }
}
