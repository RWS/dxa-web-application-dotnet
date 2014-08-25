
namespace Sdl.Web.Common.Models
{
    [SemanticEntity("http://schema.org", "MediaObject", "s")]
    public class Download : MediaItem
    {
        [SemanticProperty("s:name")]
        [SemanticProperty("s:description")]
        public string Description { get; set; }
    }
}