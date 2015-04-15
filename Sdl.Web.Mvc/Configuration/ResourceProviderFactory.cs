namespace Sdl.Web.Mvc.Configuration
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
