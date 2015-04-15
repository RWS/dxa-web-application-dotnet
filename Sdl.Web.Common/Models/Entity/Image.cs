namespace Sdl.Web.Common.Models
{
    [SemanticEntity("http://schema.org", "MediaObject", "s")]
    public class Image : MediaItem
    {
        [SemanticProperty("s:name")]
        public string AlternateText { get; set; }
    }
}