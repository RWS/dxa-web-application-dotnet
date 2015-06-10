namespace Sdl.Web.Common.Models
{
    [SemanticEntity(SchemaOrgVocabulary, "ImageObject", Prefix = "s", Public = true)]
    public class Image : MediaItem
    {
        [SemanticProperty("s:name")]
        public string AlternateText { get; set; }
    }
}