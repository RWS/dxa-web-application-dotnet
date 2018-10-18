using Sdl.Web.Common.Models;
using System.Collections.Generic;

namespace Sdl.Web.Tridion.Tests.Models.Topic
{
    [SemanticEntity(Vocab = DitaVocabulary, EntityName = "body")]
    // TODO: Support for low-level XPaths: [SemanticEntity(Vocab = XPathVocabulary, EntityName = ".//*[contains(@class, 'body ')]", Prefix ="xpath")]
    public class TestStronglyTypedTopic : EntityModel
    {
        [SemanticProperty("title")]
        public string Title { get; set; }

        [SemanticProperty("body")]
        public string Body { get; set; }

        [SemanticProperty("body")]
        public RichText BodyRichText { get; set; }

        [SemanticProperty("section")]
        public string FirstSection { get; set; }

        [SemanticProperty("section")]
        public List<string> Sections { get; set; }

        [SemanticProperty("childlink")]
        public Link FirstLink { get; set; }

        [SemanticProperty("related-links/childlink")]
        // TODO: Support for low-level XPaths: [SemanticProperty("xpath:.//*[contains(@class, 'related-links' )]//*[contains(@class, 'childlink' )]")]
        public List<Link> Links { get; set; }
    }
}
