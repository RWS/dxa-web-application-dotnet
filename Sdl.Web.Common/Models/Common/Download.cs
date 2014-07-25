
namespace Sdl.Web.Common.Models.Common
{
    [SemanticEntity("http://schema.org", "Thing", "s")]
    public class Download : MediaItem
    {
        [SemanticProperty("s:name")]
        [SemanticProperty("s:description")]
        public string Description { get; set; }
    }
}