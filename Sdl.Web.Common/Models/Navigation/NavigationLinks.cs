using System;
using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    [Serializable]
    public class NavigationLinks : EntityModel
    {
        public List<Link> Items { get; set; }

        public NavigationLinks()
        {
            Items = new List<Link>();
        }
    }
}