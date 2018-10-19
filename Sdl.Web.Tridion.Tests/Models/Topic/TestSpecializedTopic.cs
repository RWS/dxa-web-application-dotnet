using Sdl.Web.Common.Models;
using System.Collections.Generic;

namespace Sdl.Web.Tridion.Tests.Models.Topic
{
    [SemanticEntity(Vocab = DitaVocabulary, EntityName = "lcBaseBody")]
    public class TestSpecializedTopic : EntityModel
    {
        [SemanticProperty("title")]
        public string Title { get; set; }

        [SemanticProperty("lcIntro")]
        public RichText Intro { get; set; }

        [SemanticProperty("lcObjectives")]
        public RichText Objectives { get; set; }

        [SemanticProperty("lcBaseBody")]
        public TestSpecializedBody Body { get; set; }
    }

    [SemanticEntity(Vocab = DitaVocabulary)]
    public class TestSpecializedBody : EntityModel
    {
        [SemanticProperty("lcIntro")]
        public RichText Intro { get; set; }

        [SemanticProperty("lcObjectives")]
        public TestSpecializedSection Objectives { get; set; }

        [SemanticProperty("section")]
        public List<TestSpecializedSection> Sections { get; set; }
    }

    [SemanticEntity(Vocab = DitaVocabulary)]
    public class TestSpecializedSection : EntityModel
    {
        [SemanticProperty("_self")]
        public RichText Content { get; set; }
    }
}
