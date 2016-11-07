using System;
namespace Sdl.Web.Common.Models
{
    [Serializable]
    public class Link : EntityModel
    {
        [SemanticProperty(PropertyName = "internalLink")]
        [SemanticProperty(PropertyName = "externalLink")]
        public string Url { get; set; }
        public string LinkText { get; set; }
        public string AlternateText { get; set; }
    }
}