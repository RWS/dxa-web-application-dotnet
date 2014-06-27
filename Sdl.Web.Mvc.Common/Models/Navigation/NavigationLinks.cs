using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Models
{
    public class NavigationLinks : EntityBase
    {
        public List<Link> Items { get; set; }

        public NavigationLinks()
        {
            Items = new List<Link>();
        }
    }
}