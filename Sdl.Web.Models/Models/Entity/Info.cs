using System;

namespace Sdl.Web.Mvc.Models
{
    [SemanticEntity(EntityName = "CookieBar", Prefix = "cb", Vocab = Configuration.CoreVocabulary)]
    public class Info : Entity
    {
        public string Headline { get; set; }
        public string Text { get; set; }
        public string Continue { get; set; }
        public Link Link { get; set; }
    }
}
