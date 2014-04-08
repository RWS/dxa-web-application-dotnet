using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.WebPages;

namespace Sdl.Tridion.Context.Mvc {
    public class TridionContext {
        public ContextFamilyCollection FamilyCollection { get; set; }
        public object Model { get; set; }

        public static TridionContext GetInstance() {
            return new TridionContext();
        }

        private TridionContext() {
            FamilyCollection = new ContextFamilyCollection();
            FamilyCollection.Families = new List<ContextFamily>();
            FamilyCollection.Model = Model;
        }

        public TridionContext Families(Action<ContextFamilyCollection> predicate) {
            predicate(FamilyCollection);
            return this;
        }

        public MvcHtmlString Render() {
            ContextEngine context = new ContextEngine();

            StringBuilder sb = new StringBuilder();
            StringWriter writer = new StringWriter(sb);

            bool isFamilyFound = false;
            for (IEnumerator<ContextFamily> e = FamilyCollection.Families.GetEnumerator(); e.MoveNext(); ) {
                ContextFamily family = e.Current;
                if (!string.IsNullOrEmpty(context.DeviceFamily) && family.Name == context.DeviceFamily) {
                    isFamilyFound = true;
                    family.Predicate(Model).WriteTo(writer);
                }
            }

            if (!isFamilyFound && FamilyCollection.DefaultResult != null) {
                FamilyCollection.DefaultResult.WriteTo(writer);
            }

            return new MvcHtmlString(sb.ToString());
        }
    }
}