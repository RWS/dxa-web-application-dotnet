namespace Sdl.Web.Common.Models.Common
{
    public class Link : EntityBase
    {
        [SemanticProperty(PropertyName = "internalLink")]
        [SemanticProperty(PropertyName = "externalLink")]
        public string Url { get; set; }
        public string LinkText { get; set; }
        public string AlternateText { get; set; }
    }
}