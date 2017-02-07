using System;
namespace Sdl.Web.Common.Models
{
    [SemanticEntity(EntityName = "SocialLink")]
    [Serializable]
    public class TagLink : EntityModel
    {
        [SemanticProperty("internalLink")]
        [SemanticProperty("externalLink")]
        public string Url { get; set; }
        public Tag Tag { get; set; }
    }
}
