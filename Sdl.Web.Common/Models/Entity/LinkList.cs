using System.Collections.Generic;
using Sdl.Web.Common.Models.Common;

namespace Sdl.Web.Common.Models.Entity
{
    public class LinkList<T> : EntityBase
    {
        public string Headline { get; set; }
        public List<T> Links { get; set; }
        public LinkList()
        {
            Links = new List<T>();
        }
    }
}
