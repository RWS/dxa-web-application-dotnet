namespace Sdl.Web.Common.Models
{
    [SemanticEntity(Vocab = "http://schema.org", EntityName = "MediaObject", Prefix = "s", Public = true)]
    public class MediaItem : EntityBase
    {
        [SemanticProperty("s:contentUrl")]
        public string Url { get; set; }
        public string FileName { get; set; }
        [SemanticProperty("s:contentSize")]
        public int FileSize { get; set; }
        public string MimeType { get; set; }
    }
}