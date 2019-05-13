using System;
using System.Collections.Generic;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Tests.Models
{
    public class CompLinkTest2 : EntityModel
    {
        [SemanticProperty("compLink")]
        public List<Link> CompLink { get; set; }
    }
}
