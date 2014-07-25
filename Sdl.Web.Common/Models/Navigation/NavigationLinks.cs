using System.Collections.Generic;
using Sdl.Web.Common.Models.Common;

namespace Sdl.Web.Common.Models.Navigation
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