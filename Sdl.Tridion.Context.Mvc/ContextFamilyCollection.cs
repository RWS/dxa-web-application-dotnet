using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.WebPages;

namespace Sdl.Tridion.Context.Mvc {
    public class ContextFamilyCollection {
        public List<ContextFamily> Families { get; set; }
        public HelperResult DefaultResult { get; set; }
        public dynamic Model { get; set; }

        public void Bound(string name, Func<dynamic, HelperResult> predicate) {
            Families.Add(new ContextFamily() { Name = name, Predicate = predicate });
        }

        public void Default(Func<dynamic, HelperResult> predicate) {
            DefaultResult = predicate(Model);
        }
    }
}
