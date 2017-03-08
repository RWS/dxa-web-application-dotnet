using System;
using System.Collections.Generic;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Tests.Models
{
    public class CompLinkTest : EntityModel
    {
        [SemanticProperty("compLink")]
        public List<EntityModel> CompLinkAsEntityModel { get; set; }

        [SemanticProperty("compLink")]
        public List<string> CompLinkAsString { get; set; }

        [SemanticProperty("compLink")]
        public List<Link> CompLinkAsLink { get; set; }
    }
}
