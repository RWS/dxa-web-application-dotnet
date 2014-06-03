using System;

namespace Sdl.Web.Mvc.Models
{
    [SemanticEntity(EntityName = "NotificationBar", Prefix = "nb", Vocab = Configuration.CoreVocabulary)]
    public class Notification : Entity
    {
        public string Headline { get; set; }
        public string Text { get; set; }
        public string Continue { get; set; }
        public Link Link { get; set; }
    }
}
