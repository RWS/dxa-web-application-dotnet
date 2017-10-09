using System;
using System.Collections.Generic;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;

namespace Sdl.Web.Tridion
{
    /// <summary>
    /// Abstract base class for Localization Resolvers.
    /// </summary>
    public abstract class LocalizationResolver : ILocalizationResolver
    {
        private readonly IDictionary<string, Localization> _knownLocalizations = new Dictionary<string, Localization>();

        protected IDictionary<string, Localization> KnownLocalizations => _knownLocalizations;

        #region ILocalizationResolver Members
        /// <summary>
        /// Resolves a matching <see cref="Localization"/> for a given URL.
        /// </summary>
        /// <param name="url">The URL to resolve.</param>
        /// <returns>A <see cref="Localization"/> instance which base URL matches that of the given URL.</returns>
        /// <exception cref="DxaUnknownLocalizationException">If no matching Localization can be found.</exception>
        public abstract Localization ResolveLocalization(Uri url);

        /// <summary>
        /// Gets a <see cref="Localization"/> by its identifier.
        /// </summary>
        /// <param name="localizationId">The Localization identifier.</param>
        /// <returns>A <see cref="Localization"/> instance with the given identifier.</returns>
        /// <exception cref="DxaUnknownLocalizationException">If no matching Localization can be found.</exception>
        public virtual Localization GetLocalization(string localizationId)
        {
            using (new Tracer(localizationId))
            {
                Localization result;
                if (!_knownLocalizations.TryGetValue(localizationId, out result))
                {
                    throw new DxaUnknownLocalizationException($"No Localization found with ID '{localizationId}'");
                }

                return result;
            }
        }

        #endregion

        protected static bool MatchesBaseUrl(Uri url, Uri baseUrl)
        {
            return
                (url.Scheme == baseUrl.Scheme) &&
                (url.Host == baseUrl.Host) &&
                (url.Port == baseUrl.Port) &&
                url.AbsolutePath.StartsWith(baseUrl.AbsolutePath, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
