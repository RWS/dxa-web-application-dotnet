using Sdl.Web.Common.Models;
using System.Collections.Generic;

namespace Sdl.Web.Tridion.Tests.Models.Topic
{
    [SemanticEntity(Vocab = DitaVocabulary, EntityName = "body")]
    public class TestStronglyTypedTopic : EntityModel
    {
        [SemanticProperty("title")]
        public string Title { get; set; }

        [SemanticProperty("body")]
        public string Body { get; set; }

        [SemanticProperty("section")]
        public string FirstSection { get; set; }

        [SemanticProperty("section")]
        public List<string> Sections { get; set; }

        [SemanticProperty("childlink")]
        public Link FirstLink { get; set; }

        [SemanticProperty("childlink")]
        public List<Link> Links { get; set; }
    }
}
