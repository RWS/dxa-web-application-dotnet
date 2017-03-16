using System;
using System.Collections.Generic;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Tests.Models
{
    [SemanticEntity("Content")]
    public class TestEntity : EntityModel
    {
        [SemanticProperty("SingleLineText")]
        public List<string> SingleLineTextField { get; set; }

        [SemanticProperty("MultiLineText")]
        public List<string> MultiLineTextField { get; set; }

        [SemanticProperty("RichText")]
        public List<RichText> RichTextField { get; set; }

        [SemanticProperty("Date")]
        public List<DateTime> DateField { get; set; }
    }
}
