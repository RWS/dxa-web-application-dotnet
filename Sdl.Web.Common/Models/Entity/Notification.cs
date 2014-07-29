namespace Sdl.Web.Common.Models
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
