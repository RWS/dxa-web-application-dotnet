using DD4T.ContentModel.Contracts.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DD4T.ContentModel.Contracts.Configuration
{
    public interface IDD4TConfiguration
    {
        int PublicationId { get; }
        string WelcomeFile { get; }
        string ComponentPresentationController { get; }
        string ComponentPresentationAction { get; }
        string ActiveWebsite { get; }
        [Obsolete("Use SelectComponentPresentationByComponentTemplateId instead")]
        string SelectComponentByComponentTemplateId { get; }
        string SelectComponentPresentationByComponentTemplateId { get; }
        [Obsolete("Use SelectComponentPresentationByOutputFormat instead")]
        string SelectComponentByOutputFormat { get; }
        string SelectComponentPresentationByOutputFormat { get; }
        string SiteMapPath { get; }
        int BinaryHandlerCacheExpiration { get; }
        string BinaryFileExtensions { get; }
        string BinaryUrlPattern { get; }
        string BinaryFileSystemCachePath { get; }
        bool IncludeLastPublishedDate { get; }
        bool ShowAnchors { get; }
        bool LinkToAnchor { get; }
        bool UseUriAsAnchor { get; }
        bool IsPreview { get; }
        int DefaultCacheSettings { get; }
        string DataFormat { get; }
        string ContentProviderEndPoint { get; }
        string ResourcePath { get; }
        string ViewModelKeyField { get; }
        int JMSNumberOfRetriesToConnect { get; }
        int JMSSecondsBetweenRetries { get; }
        string JMSHostname { get; }
        int JMSPort { get; }
        string JMSTopic { get; }

        bool UseDefaultViewModels { get; }

        int GetExpirationForCacheRegion(string region);
    }
}
