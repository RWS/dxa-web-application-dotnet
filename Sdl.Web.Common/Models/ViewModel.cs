using System;
using System.Collections.Generic;
using System.ServiceModel.Syndication;
using Newtonsoft.Json;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Abstract base class for all (strongly typed) View Models
    /// </summary>
    [Serializable]
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
        public MvcData MvcData { get; set; }

        /// <summary>
        /// Gets or sets HTML CSS classes for use in View top level HTML element.
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public string HtmlClasses { get; set; }

        /// <summary>
        /// Gets or sets metadata used to render XPM markup
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public IDictionary<string, object> XpmMetadata { get; set; }

        /// <summary>
        /// Gets or sets extension data (additional properties which can be used by custom Model Builders, Controllers and/or Views)
        /// </summary>
        /// <value>
        /// The value is <c>null</c> if no extension data has been set.
        /// </value>
        [SemanticProperty(IgnoreMapping = true)]
        public IDictionary<string, object> ExtensionData { get; set; }

        /// <summary>
        ///  Sets an extension data key/value pair.
        /// </summary>
        /// <remarks>
        /// This convenience method ensures the <see cref="ExtensionData"/> dictionary is initialized before setting the key/value pair.
        /// </remarks>
        /// <param name="key">The key for the extension data.</param>
        /// <param name="value">The value.</param>
        public void SetExtensionData(string key, object value)
        {
            if (ExtensionData == null)
            {
                ExtensionData = new Dictionary<string, object>();
            }
            ExtensionData[key] = value;
        }

        /// <summary>
        /// Gets the rendered XPM markup
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The XPM markup.</returns>
        public abstract string GetXpmMarkup(Localization localization);

        #region Helper methods for syndication feed providers
        /// <summary>
        /// Concatenates all syndication feed items provided by a given set of feed item providers.
        /// </summary>
        /// <param name="feedItemProviders">The set of feed item providers.</param>
        /// <param name="localization">The context <see cref="Localization"/>.</param>
        /// <returns>The concatenated syndication feed items.</returns>
        protected IEnumerable<SyndicationItem> ConcatenateSyndicationFeedItems(IEnumerable<ISyndicationFeedItemProvider> feedItemProviders, Localization localization)
        {
            List<SyndicationItem> result = new List<SyndicationItem>();
            foreach (ISyndicationFeedItemProvider feedItemProvider in feedItemProviders)
            {
                result.AddRange(feedItemProvider.ExtractSyndicationFeedItems(localization));
            }
            return result;
        }

        /// <summary>
        /// Creates a syndication item link from a given <see cref="Link"/> instance.
        /// </summary>
        /// <param name="link">The <see cref="Link"/> instance.</param>
        /// <param name="localization">The context <see cref="Localization"/>.</param>
        /// <returns>The syndication item link or <c>null</c> if <paramref name="link"/> is <c>null</c> or an empty link.</returns>
        protected SyndicationLink CreateSyndicationLink(Link link, Localization localization)
        {
            if (string.IsNullOrEmpty(link?.Url))
            {
                return null;
            }
            string absoluteUrl = SiteConfiguration.MakeFullUrl(link.Url, localization);
            return new SyndicationLink(new Uri(absoluteUrl));
        }

        /// <summary>
        /// Creates a syndication feed item based on essential data.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="summary">The summary. Can be a string or a <see cref="RichText"/> instance.</param>
        /// <param name="link">The link.</param>
        /// <param name="publishDate">The date/time this item was published/created. If <c>null</c>, publish date is not included in the feed.</param>
        /// <param name="localization">The context <see cref="Localization"/>.</param>
        /// <returns>The syndication feed item.</returns>
        protected SyndicationItem CreateSyndicationItem(string title, object summary, Link link, DateTime? publishDate, Localization localization)
        {
            SyndicationItem result = new SyndicationItem
            {
                Title = new TextSyndicationContent(title),
            };

            if (summary != null)
            {
                TextSyndicationContentKind textKind = (summary is RichText) ? TextSyndicationContentKind.Html : TextSyndicationContentKind.Plaintext;
                result.Summary = new TextSyndicationContent(summary.ToString(), textKind);
            }

            SyndicationLink syndicationLink = CreateSyndicationLink(link, localization);
            if (syndicationLink != null)
            {
                result.Links.Add(syndicationLink);
            }

            if (publishDate.HasValue)
            {
                result.PublishDate = publishDate.Value;
            }

            return result;
        }
        #endregion

        /// <summary>
        /// Returns true if View Model is volatile and should not be cached
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        [JsonIgnore]
        public virtual bool IsVolatile { get; set; }

        /// <summary>
        /// Returns true if View Model has been annotated with the DxaNoCacheAttribute
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        [JsonIgnore]
        public bool HasNoCacheAttribute => Attribute.GetCustomAttribute(GetType(), typeof (DxaNoCacheAttribute)) != null;

        /// <summary>
        /// Creates a deep copy of this View Model.
        /// </summary>
        /// <returns>The copied View Model.</returns>
        public virtual ViewModel DeepCopy()
        {
            // Start with a shallow copy
            ViewModel clone = (ViewModel) MemberwiseClone();

            if (MvcData != null)
            {
                clone.MvcData = new MvcData(MvcData);
            }
            if (XpmMetadata != null)
            {
                clone.XpmMetadata = new Dictionary<string, object>(XpmMetadata);
            }
            if (ExtensionData != null)
            {
                clone.ExtensionData = new Dictionary<string, object>(ExtensionData);
            }

            return clone;
        }
    }
}
