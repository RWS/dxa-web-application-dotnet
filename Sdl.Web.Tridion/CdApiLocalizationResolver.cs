using System;
using System.Linq;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Delivery.Model.Mapping;
using Tridion.ContentDelivery.DynamicContent;

namespace Sdl.Web.Tridion
{
    /// <summary>
    /// Localization Resolver that uses the CDaaS API to do the URL to Publication mapping.
    /// </summary>
    /// <remarks>
    /// This class is excluded in the Release_7.1 configuration.
    /// </remarks>
    public class CdApiLocalizationResolver : LocalizationResolver
    {
        private readonly DynamicMappingsRetriever _mappingsRetriever = new DynamicMappingsRetriever();

        /// <summary>
        /// Initializes a new <see cref="CdApiLocalizationResolver"/> instance.
        /// </summary>
        public CdApiLocalizationResolver()
        {
            using (new Tracer())
            {
            }
        }

        #region ILocalizationResolver Members
        /// <summary>
        /// Resolves a matching <see cref="Localization"/> for a given URL.
        /// </summary>
        /// <param name="url">The URL to resolve.</param>
        /// <returns>A <see cref="Localization"/> instance which base URL matches that of the given URL.</returns>
        /// <exception cref="DxaUnknownLocalizationException">If no matching Localization can be found.</exception>
        public override Localization ResolveLocalization(Uri url)
        {
            using (new Tracer(url))
            {
                string urlLeftPart = url.GetLeftPart(UriPartial.Path);
                // TODO PERF: to optimize caching, we could only take the first part of the URL path (e.g. one or two levels)

                IPublicationMapping mapping = null;
                try
                {
                    // NOTE: we're not using UrlToLocalizationMapping here, because we may match too eagerly on a base URL when there is a matching mapping with a more specific URL.
                    mapping = _mappingsRetriever.GetPublicationMapping(urlLeftPart);
                }
                catch (Exception ex)
                {
                    // CIL throws Sdl.Web.Delivery.Service.InvalidResourceException if the mapping cannot be resolved.
                    // We don't have a direct reference to Sdl.Web.Delivery.Service, so we just check the type name
                    if (ex.GetType().FullName != "Sdl.Web.Delivery.Service.InvalidResourceException")
                    {
                        throw;
                    }
                    Log.Debug("Exception occurred in DynamicMappingsRetriever.GetPublicationMapping('{0}'):\n{1}", urlLeftPart, ex.ToString());
                    // Let mapping be null, we'll handle it below.
                }
                if (mapping == null || mapping.Port != url.Port.ToString()) // See CRQ-1195
                {
                    throw new DxaUnknownLocalizationException(string.Format("No matching Localization found for URL '{0}'", urlLeftPart));
                }

                Localization result;
                lock (KnownLocalizations)
                {
                    string localizationId = mapping.PublicationId.ToString();
                    if (!KnownLocalizations.TryGetValue(localizationId, out result))
                    {
                        result = new Localization
                        {
                            LocalizationId = localizationId,
                            Path = mapping.Path
                        };
                        KnownLocalizations.Add(localizationId, result);
                    }
                }

                result.EnsureInitialized();
                return result;
            }
        }
        #endregion
    }
}
