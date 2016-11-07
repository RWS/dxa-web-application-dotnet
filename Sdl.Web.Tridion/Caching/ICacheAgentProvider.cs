using DD4T.ContentModel.Contracts.Caching;

namespace Sdl.Web.Tridion.Caching
{
    /// <summary>
    /// DD4T Cache Agent provider interface
    /// </summary>
    public interface ICacheAgentProvider
    {
        ICacheAgent CacheAgent { get; }
    }
}
