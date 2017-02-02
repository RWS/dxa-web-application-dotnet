using System;
using System.Collections.Generic;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Tests.Models
{
    [SemanticEntity(EntityName = "TSI811")]
    public class Tsi811TestEntity : EntityModel
    {
        public List<Tsi811TestKeyword> Keyword1 { get; set; }
        public KeywordModel Keyword2 { get; set; }

        [SemanticProperty("booleanKeyword")]
        public bool BooleanProperty { get; set; }
    }

    [SemanticEntity(EntityName = "TSI811KeywordMetadataSchema")]
    public class Tsi811TestKeyword : KeywordModel
    {
        public string TextField { get; set; }

        [SemanticProperty("numberField")]
        public double NumberProperty { get; set; }

        public DateTime? DateField { get; set; }
        public List<Tag> KeywordField { get; set; }
    }
}
