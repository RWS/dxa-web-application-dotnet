using System.ServiceModel.Syndication;
using System.Text;
using System.Web;
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
            HttpResponseBase response = context.HttpContext.Response;

            response.ContentType = ContentType;
            response.ContentEncoding = ContentEncoding;

            using (XmlTextWriter writer = new XmlTextWriter(response.Output))
            {
                writer.Formatting = Formatting.Indented;
                Formatter.WriteTo(writer);
            }
        }
    }
}
