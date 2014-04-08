using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    public class Entity
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public Semantics Semantics { get; set; }
    }
}