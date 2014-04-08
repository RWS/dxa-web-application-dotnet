using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Sdl.Tridion.Context.Mvc {
    public static class TridionHtmlHelper {

        public static TridionContext Tridion(this HtmlHelper helper) {
            return TridionContext.GetInstance();
        }

        public static TridionContext Tridion(this HtmlHelper helper, object model) {
            TridionContext context = TridionContext.GetInstance();
            context.Model = model;
            return context;
        }
    }
}