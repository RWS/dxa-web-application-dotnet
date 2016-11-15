using System;
namespace Sdl.Web.Common.Models
{
    [Serializable]
    public class TagLink : EntityModel
    {
        [SemanticProperty(PropertyName = "internalLink")]
        [SemanticProperty(PropertyName = "externalLink")]
        public string Url { get; set; }
        public Tag Tag { get; set; }
    }
}
