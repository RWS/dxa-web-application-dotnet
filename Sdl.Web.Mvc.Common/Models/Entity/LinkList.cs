using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc.Models
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
