using System.Collections.Generic;

namespace Sdl.Web.Common.Models
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
