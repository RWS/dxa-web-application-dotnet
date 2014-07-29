using System.Collections.Generic;

namespace Sdl.Web.Common.Models
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