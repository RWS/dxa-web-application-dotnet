using System;
namespace Sdl.Web.Common.Models
{
    [SemanticEntity(EntityName = "EmbeddedLink")]
    [Serializable]
    public class Link : EntityModel
    {
        [SemanticProperty("internalLink")]
        [SemanticProperty("externalLink")]
        public string Url { get; set; }
        public string LinkText { get; set; }
        public string AlternateText { get; set; }
    }
}