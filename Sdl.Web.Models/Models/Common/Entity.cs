using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    public class Entity
    {
        public Semantics Semantics { get; set; }
        public Dictionary<string, string> EntityData { get; set; }
        public Dictionary<string, string> PropertyData { get; set; }
    }
}