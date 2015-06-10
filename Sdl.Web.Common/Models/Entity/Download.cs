
namespace Sdl.Web.Common.Models
{
    [SemanticEntity(SchemaOrgVocabulary, "DataDownload", Prefix = "s", Public = true)]
    public class Download : MediaItem
    {
        [SemanticProperty("s:name")]
        [SemanticProperty("s:description")]
        public string Description { get; set; }
    }
}