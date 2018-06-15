using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Mapping;

namespace Sdl.Web.Common.Interfaces
{
    public interface ILocalization
    {
        /// <summary>
        /// Gets the Localization Identifier.
        /// </summary>
        /// <remarks>
        /// This corresponds to the (numeric) CM Publication Identifier. That is: the middle number in the Publication TCM URI.
        /// </remarks>
        string Id { get; set; }

        /// <summary>
        /// Gets or sets the URL Path of the Localization.
        /// </summary>
        /// <value>
        /// Is empty for a root-level Localization. It never ends with a slash.
        /// </value>
        /// <remarks>
        /// This property should only be set by the DXA Framework itself (in particular: by Localization Resolvers).
        /// </remarks>
        string Path { get; set; }

        /// <summary>
        /// Gets the Culture/Locale of the Localization as a string value.
        /// </summary>
        /// <remarks>
        /// The value is obtained from CM: the <c>core.culture</c> configuration value.
        /// It is used by the <see cref="CultureInfo"/> property and also as Language (!) of Atom/RSS feeds.
        /// For that reason, it must be a valid language tag as defined by Microsoft: https://msdn.microsoft.com/en-us/library/cc233982.aspx
        /// </remarks>
        /// <seealso cref="CultureInfo"/>
        string Culture { get; }

        /// <summary>
        /// Get the Culture/Locale of the Localization as a <see cref="CultureInfo"/> object.
        /// </summary>
        /// <remarks>
        /// The Culture/Locale is used to format dates (e.g. by <see cref="HtmlHelperExtensions.Date"/>) and numbers.
        /// </remarks>
        /// <seealso cref="Culture"/>
        CultureInfo CultureInfo { get; }

        /// <summary>
        /// Gets the Language of the Localization.
        /// </summary>
        /// <remarks>
        /// The value is obtained from CM: the <c>core.language</c> configuration value.
        /// Is used for display purposes and doesn't have to conform to any standard.
        /// </remarks>
        /// <seealso cref="Culture"/>
        string Language { get; set; }

        /// <summary>
        /// Gets the URI scheme used for CM URIs.
        /// </summary>
        /// <remarks>
        /// Is always "tcm" for now, but can also become "ish" in the future (KC Web App support).
        /// </remarks>
        string CmUriScheme { get; }

        /// <summary>
        /// Gets the URL pattern (Regular Expression) used to determine if a URL represents a Static Content Item.
        /// </summary>
        string StaticContentUrlPattern { get; }

        /// <summary>
        /// Gets the root folder of the binaries cache for this Localization.
        /// </summary>
        string BinaryCacheFolder { get; }

        /// <summary>
        /// Gets (or sets) whether the Localization is XPM Enabled (a.k.a. a "Staging" environment).
        /// </summary>
        bool IsXpmEnabled { get; set; }

        /// <summary>
        /// Gets whether the Localization has an HTML Design which is published from CM.
        /// </summary>
        bool IsHtmlDesignPublished { get; }

        /// <summary>
        /// Gets whether the Localization is the default one in the set of "Site Localizations"
        /// </summary>
        /// <seealso cref="SiteLocalizations"/>
        bool IsDefaultLocalization { get; set; }

        /// <summary>
        /// Gets the version of the HTML Design.
        /// </summary>
        /// <remarks>
        /// The version is obtained from a <c>version.json</c> file.
        /// </remarks>
        /// <seealso cref="IsHtmlDesignPublished"/>
        string Version { get; }

        /// <summary>
        /// Gets the Data Formats supported in this Localization.
        /// </summary>
        List<string> DataFormats { get; }

        /// <summary>
        /// Gets the "Site Localizations": a list of Localizations in the same "Site Group".
        /// </summary>
        /// <remarks>
        /// A typical use case is a multi-language site consisting of separate Localizations for each language.
        /// </remarks>
        List<ILocalization> SiteLocalizations { get; }

        /// <summary>
        /// Gets the date/time at which this <see cref="ILocalization"/> was last (re-)loaded.
        /// </summary>
        DateTime LastRefresh { get; }

        /// <summary>
        /// Ensures that the <see cref="ILocalization"/> is initialized.
        /// </summary>
        void EnsureInitialized();

        /// <summary>
        /// Forces a refresh/reload of the <see cref="ILocalization"/> and its associated configuration.
        /// </summary>
        void Refresh(bool allSiteLocalizations = false);

        /// <summary>
        /// Gets a configuration value with a given key.
        /// </summary>
        /// <param name="key">The configuration key, in the format section.name.</param>
        /// <returns>The configuration value.</returns>
        string GetConfigValue(string key);

        /// <summary>
        /// Gets resources.
        /// </summary>
        /// <param name="sectionName">Optional name of the section for which to get resource. If not specified (or <c>null</c>), all resources are obtained.</param>
        IDictionary GetResources(string sectionName = null);

        /// <summary>
        /// Gets an absolute (server-relative) URL path for a given context-relative URL path.
        /// </summary>
        /// <param name="contextRelativeUrlPath">The context-relative URL path. Should not start with a slash.</param>
        /// <returns>The absolute URL path.</returns>
        string GetAbsoluteUrlPath(string contextRelativeUrlPath);

        /// <summary>
        /// Gets a versioned URL (including the version number of the HTML design/assets).
        /// </summary>
        /// <param name="relativePath">The (unversioned) URL path relative to the system folder</param>
        /// <returns>A versioned URL path (server-relative).</returns>
        /// <remarks>
        /// Versioned URLs are used to facilitate agressive caching of those assets; see StaticContentModule.
        /// </remarks>
        string GetVersionedUrlPath(string relativePath);

        /// <summary>
        /// Gets the include Page URLs for a given Page Type/Template.
        /// </summary>
        /// <param name="pageTypeIdentifier">The Page Type Identifier.</param>
        /// <returns>The URLs of Include Pages</returns>
        /// <remarks>
        /// The concept of Include Pages will be removed in a future version of DXA.
        /// As of DXA 1.1 Include Pages are represented as <see cref="Sdl.Web.Common.Models.PageModel.Regions"/>.
        /// Implementations should avoid using this method directly.
        /// </remarks>
        IEnumerable<string> GetIncludePageUrls(string pageTypeIdentifier);

        /// <summary>
        /// Gets XPM Region configuration for a given Region name.
        /// </summary>
        /// <param name="regionName">The Region name</param>
        /// <returns>The XPM Region configuration or <c>null</c> if no configuration is found.</returns>
        XpmRegion GetXpmRegionConfiguration(string regionName);

        /// <summary>
        /// Gets Semantic Schema for a given schema identifier.
        /// </summary>
        /// <param name="schemaId">The schema identifier.</param>
        /// <returns>The Semantic Schema configuration.</returns>
        SemanticSchema GetSemanticSchema(string schemaId);

        /// <summary>
        /// Gets the Semantic Vocabularies
        /// </summary>
        /// <returns></returns>
        IEnumerable<SemanticVocabulary> GetSemanticVocabularies();

        /// <summary>
        /// Gets a Semantic Vocabulary by a given prefix.
        /// </summary>
        /// <param name="prefix">The vocabulary prefix.</param>
        /// <returns>The Semantic Vocabulary.</returns>
        SemanticVocabulary GetSemanticVocabulary(string prefix);

        /// <summary>
        /// Gets a CM identifier (URI) for this Localization
        /// </summary>
        /// <returns>The CM URI.</returns>
        string GetCmUri();

        /// <summary>
        /// Gets a CM identifier (URI) for a given Model identifier.
        /// </summary>
        /// <param name="modelId">The Model identifier.</param>
        /// <param name="itemType">The item type identifier used in the CM URI.</param>
        /// <returns>The CM URI.</returns>
        string GetCmUri(string modelId, int itemType = 16);

        /// <summary>
        /// Gets the base URI for this localization
        /// </summary>
        /// <returns>The Base URI.</returns>
        string GetBaseUrl();

        /// <summary>
        /// Determines whether a given URL (path) refers to a static content item.
        /// </summary>
        /// <param name="urlPath">The URL path.</param>
        /// <returns><c>true</c> if the URL refers to a static content item.</returns>
        bool IsStaticContentUrl(string urlPath);

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        string ToString();

        /// <summary>
        /// Loads and deserializes static content items used by this localization. Used to load resources/configuration/schema 
        /// when initializing the localization.
        /// </summary>
        /// <typeparam name="T">Type of object to deserialize</typeparam>
        /// <param name="relativeUrl">Relative Url of resource to load</param>
        /// <param name="deserializedObject">Deserialized object</param>
        void LoadStaticContentItem<T>(string relativeUrl, ref T deserializedObject);
    }
}
