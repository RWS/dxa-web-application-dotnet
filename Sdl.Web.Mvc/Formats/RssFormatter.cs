using System.ServiceModel.Syndication;
using System.Web.Mvc;

namespace Sdl.Web.Mvc.Formats
{
    public class RssFormatter : FeedFormatter
    {
        public RssFormatter()
        {
            AddMediaType("application/rss+xml");
            ProcessModel = true;
            AddIncludes = false;
        }

        public override ActionResult FormatData(ControllerContext controllerContext, object model)
        {
            SyndicationFeed feed = GetData(model);
            return feed == null ? null : new FeedResult(new Rss20FeedFormatter(feed)) { ContentType = "application/rss+xml" };
        }
    }
}
