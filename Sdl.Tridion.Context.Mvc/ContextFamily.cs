using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.WebPages;

namespace Sdl.Tridion.Context.Mvc {
    public class ContextFamily {
        public string Name { get; set; }
        public Func<dynamic, HelperResult> Predicate;
    }
}
