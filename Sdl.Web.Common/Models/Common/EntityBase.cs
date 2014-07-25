using System.Collections.Generic;
using Sdl.Web.Common.Models.Interfaces;

namespace Sdl.Web.Common.Models.Common
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