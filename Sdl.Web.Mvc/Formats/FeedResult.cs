using System.ServiceModel.Syndication;
using System.Text;
using System.Web.Mvc;
using System.Xml;

namespace Sdl.Web.Mvc.Formats
{
    public class FeedResult : ActionResult
    {
        public SyndicationFeedFormatter Formatter { get; set; }
        public Encoding ContentEncoding { get; set; }
        public string ContentType { get; set; }

        public FeedResult(SyndicationFeedFormatter formatter)
        {
            Formatter = formatter;
            ContentEncoding = Encoding.UTF8;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            var response = context.HttpContext.Response;

            response.ContentType = ContentType;
            response.ContentEncoding = ContentEncoding;

            using (var writer = new XmlTextWriter(response.Output))
            {
                writer.Formatting = Formatting.Indented;
                Formatter.WriteTo(writer);
            }
        }
    }
}
