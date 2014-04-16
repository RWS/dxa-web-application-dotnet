using DD4T.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.DD4T
{
    public class ExtensionlessLinkFactory : LinkFactory
    {
        public string ResolveExtensionlessLink(string componentUri)
        {
            return RemoveExtension(base.ResolveLink(componentUri));
        }

        protected virtual string RemoveExtension(string url)
        {
            if (url != null)
            {
                var pos = url.LastIndexOf(".");
                if (pos > url.LastIndexOf("/"))
                {
                    url = url.Substring(0, pos);
                }
            }
            return url;
        }
    }
}
