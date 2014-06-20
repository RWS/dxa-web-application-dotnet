using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Site.Controllers
{
    public class ResolverController : BaseController
    {
        public ResolverController(IContentProvider contentProvider, IRenderer renderer)
        {
            ContentProvider = contentProvider;
            Renderer = renderer;
        }
        public ActionResult Resolve(string itemId, string localization)
        {
            //TODO remove this tcm specific code here
            var url = ContentProvider.ProcessUrl("tcm:" + itemId, localization);
            if (url == null)
            {
                var bits = itemId.Split(':');
                if (bits.Length > 1)
                {
                    bits = bits[1].Split('-');
                    int pubid = 0;
                    if (Int32.TryParse(bits[0], out pubid))
                    {
                        foreach (var loc in Configuration.Localizations.Values)
                        {
                            if (loc.LocalizationId == pubid)
                            {
                                url = loc.Path;
                            }
                        }
                    }
                }
            }
            if (url == null)
            {
                if (localization == null)
                {
                    url = Configuration.DefaultLocalization;
                }
                else
                {
                    var loc = Configuration.Localizations.Values.Where(l => l.LocalizationId.ToString() == localization).FirstOrDefault();
                    if (loc != null)
                    {
                        url = loc.Path;
                    }
                }
            }
            Response.Redirect(url, true);
            return null;
        }

    }
}
