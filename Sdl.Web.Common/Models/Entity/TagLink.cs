namespace Sdl.Web.Common.Models
{
    public class TagLink : EntityBase
    {
        [SemanticProperty(PropertyName = "internalLink")]
        [SemanticProperty(PropertyName = "externalLink")]
        public string Url { get; set; }
        public Tag Tag { get; set; }
    }
}
