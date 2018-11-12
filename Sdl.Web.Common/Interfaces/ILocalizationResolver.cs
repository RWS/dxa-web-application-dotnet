using System;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Common.Interfaces
{
    /// <summary>
    /// Interface used for Localization Resolver extension point.
    /// </summary>
    public interface ILocalizationResolver
    {
        /// <summary>
        /// Resolves a matching <see cref="ILocalization"/> for a given URL.
        /// </summary>
        /// <param name="url">The URL to resolve.</param>
        /// <returns>A <see cref="ILocalization"/> instance which base URL matches that of the given URL.</returns>
        /// <exception cref="DxaUnknownLocalizationException">If no matching Localization can be found.</exception>
        Localization ResolveLocalization(Uri url);

        /// <summary>
        /// Gets a <see cref="ILocalization"/> by its identifier.
        /// </summary>
        /// <param name="localizationId">The Localization identifier.</param>
        /// <returns>A <see cref="ILocalization"/> instance with the given identifier.</returns>
        /// <exception cref="DxaUnknownLocalizationException">If no matching Localization can be found.</exception>
        Localization GetLocalization(string localizationId);
    }
}
