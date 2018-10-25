using System;
using Sdl.Tridion.Api.Client.ContentModel;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Tridion.PCAClient;

namespace Sdl.Web.Tridion
{
    public class GraphQLLocalizationResolver : LocalizationResolver
    {
        /// <summary>
        /// Resolves a matching <see cref="ILocalization"/> for a given URL.
        /// </summary>
        /// <param name="url">The URL to resolve.</param>
        /// <returns>A <see cref="ILocalization"/> instance which base URL matches that of the given URL.</returns>
        /// <exception cref="DxaUnknownLocalizationException">If no matching Localization can be found.</exception>
        public override ILocalization ResolveLocalization(Uri url)
        {
            using (new Tracer(url))
            {
                string urlLeftPart = url.GetLeftPart(UriPartial.Path);

                // TODO PERF: to optimize caching, we could only take the first part of the URL path (e.g. one or two levels)
                int espaceIndex = urlLeftPart.IndexOf("%");
                if (espaceIndex > 0)
                {
                    // TODO: This is a work-around for a bug in SDL Web 8 Publication Mapping: URLs with escaped characters don't resolve properly (CRQ-1585).
                    // Therefore we truncate the URL at the first escaped character for now (assuming that the URL is still specific enough to resolve the right Publication).
                    urlLeftPart = urlLeftPart.Substring(0, espaceIndex);
                }

                // NOTE: we're not using UrlToLocalizationMapping here, because we may match too eagerly on a base URL when there is a matching mapping with a more specific URL.
                PublicationMapping mapping = PCAClientFactory.Instance.CreateClient().GetPublicationMapping(
                        ContentNamespace.Sites,
                        urlLeftPart);

                mapping = new PublicationMapping {PublicationId = 5, Port = "8882"};

                if (mapping == null || mapping.Port != url.Port.ToString()) // See CRQ-1195
                {
                    throw new DxaUnknownLocalizationException($"No matching Localization found for URL '{urlLeftPart}'");
                }

                ILocalization result;
                lock (KnownLocalizations)
                {
                    string localizationId = mapping.PublicationId.ToString();
                    if (!KnownLocalizations.TryGetValue(localizationId, out result))
                    {
                        result = new Localization
                        {
                            Id = localizationId,
                            Path = mapping.Path
                        };
                        KnownLocalizations.Add(localizationId, result);
                    }
                    else
                    {
                        // we fill in the path regardless as it may of been
                        // a partially created localization.
                        result.Path = mapping.Path;
                    }
                }

                result.EnsureInitialized();
                return result;
            }
        }

        public override ILocalization GetLocalization(string localizationId)
        {
            using (new Tracer(localizationId))
            {
                ILocalization result;
                if (!KnownLocalizations.TryGetValue(localizationId, out result))
                {
                    // No localization found so lets return a partially constructed one and fully resolve it later.
                    result = new Localization
                    {
                        Id = localizationId
                    };
                }

                return result;
            }
        }
    }
}
