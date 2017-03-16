using Sdl.Web.Common.Configuration;
using System;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Base class for View Models representing a Keyword in CM.
    /// </summary>
    /// <remarks>
    /// This class can be used as an alternative for class <see cref="Tag"/>; it provides direct access to the Keyword's Id, Title, Description and Key.
    /// You can also create a subclass with additional properties in case your Keyword has custom metadata which you want to use in your View.
    /// Regular semantic mapping can be used to map the Keyword's metadata fields to properties of your subclass.
    /// </remarks>
    [Serializable]
    public class KeywordModel : ViewModel
    {
        /// <summary>
        /// Gets or sets the identifier for the Keyword.
        /// </summary>
        /// <remarks>
        /// The identifier represents the Item ID part of the Keyword TCM URI.
        /// </remarks>
        [SemanticProperty(IgnoreMapping = true)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the title of the Keyword
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description of the Keyword
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the key of the Keyword
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the Taxonomy identifier
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public string TaxonomyId { get; set; }

        #region Overrides
        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string containing the type and identifier of the Entity.
        /// </returns>
        public override string ToString()
            => $"{GetType().Name}: {Id}";

        public override string GetXpmMarkup(Localization localization)
            => string.Empty;

        #endregion
    }
}
