using System;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Tests.Models
{
    [SemanticEntity(Vocab = CoreVocabulary, EntityName = "SimpleTestEntity", Prefix = "s")]
    [SemanticEntity(Vocab = CoreVocabulary, EntityName = "FolderSchema", Prefix = "f")]
    public class Tsi2844TestEntity : EntityModel
    {
        [SemanticProperty("s:singleLineText")]
        public string SingleLineText { get; set; }

        [SemanticProperty("s:metadataTextField")]
        public string MetadataTextField { get; set; }

        [SemanticProperty("f:folderMetadataTextField")]
        public string FolderMetadataTextField { get; set; }
    }
}
