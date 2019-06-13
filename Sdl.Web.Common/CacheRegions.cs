using System;
using System.Web.Configuration;

namespace Sdl.Web.Common
{
    /// <summary>
    /// Constants for the names of Cache Regions used by the DXA Framework. Cache regions allow caching of
    /// specific items to be grouped under a particular cache region. Cache regions can then be configured
    /// to use different caches.
    /// </summary>
    public static class CacheRegions
    {
        public const string PageModel = "PageModel";
        public const string IncludePageModel = "IncludePageModel";
        public const string EntityModel = "EntityModel";
        public const string StaticNavigation = "Navigation_Static";
        public const string DynamicNavigation = "Navigation_Dynamic";
        public const string NavigationTaxonomy = "NavTaxonomy";
        public const string Page = "Page"; // DD4T Page
        public const string ComponentPresentation = "ComponentPresentation"; // DD4T ComponentPresentation
        public const string Other = "Other"; // Other DD4T object
        public const string BinaryPublishDate = "BinaryPublishDate";
        public const string Binary = "Binary";
        public const string ModelService = "ModelService";
        public const string PublicationMapping = "PublicationMapping";
        public const string LinkResolving = "LinkResolving";
        public const string BrokerQuery = "BrokerQuery";
        public const string RenderedOutput = "RenderedOutput";

        /// <summary>
        /// Returns true if view model caching is enabled in the web applications configuration.
        /// <example>
        /// <appSettings>
        ///      <add key="viewModel-caching" value="true"/>
        /// </appSettings>
        /// </example>
        /// </summary>
        public static bool IsViewModelCachingEnabled { get; private set; }

        static CacheRegions()
        {
            string cachingSetting = WebConfigurationManager.AppSettings["viewModel-caching"];
            IsViewModelCachingEnabled = !string.IsNullOrEmpty(cachingSetting) && cachingSetting.Equals("true", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
