using System.Collections.Generic;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Tests.Models
{
    public abstract class Tsi1757TestEntity : EntityModel
    {
    }

    [SemanticEntity(Prefix = "s1", Vocab = "uuid:a3cbac3c-375a-43d3-937e-788110d6b9ee", EntityName = "Content")]
    public class Tsi1757TestEntity1 : Tsi1757TestEntity
    {
        [SemanticProperty("s1:textField")]
        public string TextField { get; set; }

        [SemanticProperty("s1:embeddedTextField")]
        public string EmbeddedTextField { get; set; }
    }

    [SemanticEntity(Prefix = "s2", Vocab = "uuid:3a50c82e-f113-4e7d-94f5-23359b3b0a4e", EntityName = "Content")]
    public class Tsi1757TestEntity2 : Tsi1757TestEntity
    {
        [SemanticProperty("s2:textField")]
        public string TextField { get; set; }
    }

    [SemanticEntity(Prefix = "s3", Vocab = "uuid:e6b74fb5-d407-4baa-ab53-ef1bc0b72887", EntityName = "Content")]
    public class Tsi1757TestEntity3 : Tsi1757TestEntity
    {
        [SemanticProperty("s3:textField")]
        public string TextField { get; set; }

        [SemanticProperty("s3:compLinkField")]
        public List<Tsi1757TestEntity> CompLinkField { get; set; }
    }
}
