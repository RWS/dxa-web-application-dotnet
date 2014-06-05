using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc
{
    public class ResourceProviderFactory : System.Web.Compilation.ResourceProviderFactory
    {
        public override System.Web.Compilation.IResourceProvider CreateGlobalResourceProvider(string classKey)
        {
            return new ResourceProvider();
        }

        public override System.Web.Compilation.IResourceProvider CreateLocalResourceProvider(string virtualPath)
        {
            return new ResourceProvider();
        }
    }
}
