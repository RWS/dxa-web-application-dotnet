using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    public class Link : Entity
    {
        public string Url { get; set; }
        public string LinkText { get; set; }
        public string AlternateText { get; set; }
        public string Id { get; set; }
    }
}