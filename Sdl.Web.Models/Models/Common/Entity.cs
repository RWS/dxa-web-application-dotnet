using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    public class Entity
    {
        [SemanticProperty(IgnoreMapping = true)]
        public Dictionary<string, string> EntityData { get; set; }
        [SemanticProperty(IgnoreMapping = true)]
        public Dictionary<string, string> PropertyData { get; set; }
    }
}