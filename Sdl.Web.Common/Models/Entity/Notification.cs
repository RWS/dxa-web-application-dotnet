using Sdl.Web.Common.Models.Common;

namespace Sdl.Web.Common.Models.Entity
{
    [SemanticEntity(EntityName = "NotificationBar", Prefix = "nb", Vocab = CoreVocabulary)]
    public class Notification : EntityBase
    {
        public string Headline { get; set; }
        public string Text { get; set; }
        public string Continue { get; set; }
        public Link Link { get; set; }
    }
}
