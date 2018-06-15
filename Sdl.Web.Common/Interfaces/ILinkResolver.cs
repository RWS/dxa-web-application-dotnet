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
        /// <param name="resolveToBinary">Specifies whether a link to a Multimedia Component should be resolved directly to its Binary (<c>true</c>) or as a regular Component link.</param>
        /// <param name="localization">The context Localization (optional, since the TCM URI already contains a Publication ID, but this allows resolving in a different context).</param>
        /// <returns>The resolved URL.</returns>
        string ResolveLink(string sourceUri, bool resolveToBinary = false, ILocalization localization = null);
    }
}
