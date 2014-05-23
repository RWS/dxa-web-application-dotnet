using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    public class Breadcrumb
    {
        //TODO: consider logic to exclude default/index page from breadcrumb
        public List<Link> Items { get; set; }

        public Breadcrumb()
        {
            Items = new List<Link>();
        }
    }
}