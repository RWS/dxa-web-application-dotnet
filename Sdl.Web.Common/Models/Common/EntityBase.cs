using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    public class EntityBase : IEntity
    {
        [SemanticProperty(IgnoreMapping = true)]
        public Dictionary<string, string> EntityData { get; set; }
        [SemanticProperty(IgnoreMapping = true)]
        public Dictionary<string, string> PropertyData { get; set; }
        [SemanticProperty(IgnoreMapping = true)]
        public string Id { get; set; }
        public const string CoreVocabulary = "http://www.sdl.com/web/schemas/core";
    }
}