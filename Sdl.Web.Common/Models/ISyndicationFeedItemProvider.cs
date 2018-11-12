using System.Collections.Generic;
using System.ServiceModel.Syndication;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Interface implemented by View Model Types that can provide syndication feed items.
    /// </summary>
    public interface ISyndicationFeedItemProvider
    {
        IEnumerable<SyndicationItem> ExtractSyndicationFeedItems(Localization localization);
    }
}
