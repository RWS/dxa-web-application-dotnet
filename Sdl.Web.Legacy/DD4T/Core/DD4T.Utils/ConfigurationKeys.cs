using System;
using System.Configuration;
using DD4T.ContentModel.Contracts.Providers;

namespace DD4T.Utils
{
    public static class ConfigurationKeys
    {

        // the Alt1, Alt2 versions are there to make the system backwards compatible
        // note that the associated properties in the ConfigurationHelper must be changed as well
        public const string ProviderVersion = "DD4T.ProviderVersion";
        public const string LoggerClass = "DD4T.LoggerClass";
        public const string IncludeLastPublishedDate = "DD4T.IncludeLastPublishedDate";
        public const string BinaryHandlerCacheExpiration = "DD4T.BinaryHandlerCacheExpiration";
        public const string BinaryHandlerCacheExpirationAlt1 = "BinaryHandlerCaching";
        public const string BinaryFileExtensions = "DD4T.BinaryFileExtensions";
        public const string BinaryFileExtensionsAlt1 = "BinaryFileExtensions";
        public const string ComponentPresentationController = "DD4T.ComponentPresentationController";
        public const string ComponentPresentationControllerAlt1 = "Controller";
        public const string ComponentPresentationAction = "DD4T.ComponentPresentationAction";
        public const string ComponentPresentationActionAlt1 = "Action";
        public const string SitemapPath = "DD4T.SitemapPath";
        public const string SitemapPathAlt1 = "SitemapPath";
        public const string SelectComponentByComponentTemplateId = "ComponentFactory.ComponentTemplateId";
        public const string SelectComponentByComponentTemplateIdAlt1 = "DD4T.SelectComponentByComponentTemplateId";
        public const string SelectComponentByOutputFormat = "ComponentFactory.OutputFormat";
        public const string SelectComponentByOutputFormatAlt1 = "DD4T.SelectComponentByOutputFormat";
        public const string ActiveWebsite = "DD4T.Site.ActiveWebSite";
        public const string ActiveWebsiteAlt1 = "Site.ActiveWebSite";
        public const string ShowAnchors = "DD4T.ShowAnchors";
        public const string LinkToAnchor = "DD4T.LinkToAnchor";
        public const string UseUriAsAnchor = "DD4T.UseUriAsAnchor";
        public const string PublicationId = "DD4T.PublicationId";
        public const string BinaryUrlPattern = "DD4T.BinaryUrlPattern";
        public const string WelcomeFile = "DD4T.WelcomeFile";
        public const string WelcomeFileAlt1 = "DD4T.DefaultPage";
        public const string DataFormat = "DD4T.DataFormat";
        public const string ResourcePath = "DD4T.ResourcePath";
        public const string ViewModelKeyFieldName = "DD4T.ViewModels.ViewModelKeyFieldName";
        public const string ContentProviderEndPoint = "DD4T.ContentProviderEndPoint";
        public const string IsPreview = "DD4T.IsPreview";
        public const string UseDefaultViewModels = "DD4T.UseDefaultViewModels";
        

        // JMS settings
        public const string JMSHostname = "DD4T.JMS.Hostname";
        public const string JMSPort = "DD4T.JMS.Port";
        public const string JMSTopic = "DD4T.JMS.Topic";
        public const string JMSNumberOfRetriesToConnect = "DD4T.JMS.NumberOfRetriesToConnect";
        public const string JMSSecondsBetweenRetries = "DD4T.JMS.SecondsBetweenRetries";

        // Caching settings
        public const string DefaultCacheSettings = "DD4T.CacheSettings.Default";
        public const string CacheSettingsPerRegion = "DD4T.CacheSettings.{0}";
        public const string BinaryFileSystemCachePath = "DD4T.BinaryFileSystemCachePath";
        
    }
}
