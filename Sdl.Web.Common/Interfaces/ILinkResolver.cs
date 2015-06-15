namespace Sdl.Web.Common.Interfaces
{
    /// <summary>
    /// Interface for Link Resolver extension point.
    /// </summary>
    public interface ILinkResolver
    {
        
        /// <summary>
        /// Resolves a link URI (TCM URI or site URL) to a normalized site URL.
        /// </summary>
        /// <param name="sourceUri">The source URI (TCM URI or site URL)</param>
        /// <returns>The resolved URL.</returns>
        string ResolveLink(string sourceUri);

        /// <summary>
        /// Resolves a link URI (TCM URI or site URL) to a normalized site URL in context of a given Localization.
        /// </summary>
        /// <param name="sourceUri">The source URI (TCM URI or site URL)</param>
        /// <param name="localizationId">The Localization ID.</param>
        /// <returns>The resolved URL.</returns>
        string ResolveLink(string sourceUri, int localizationId);
     
    }
}
