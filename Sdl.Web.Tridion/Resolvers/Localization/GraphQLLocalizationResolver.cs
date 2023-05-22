using System;
using System.Collections.Concurrent;
using Sdl.Tridion.Api.Client.ContentModel;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Tridion.ApiClient;

namespace Sdl.Web.Tridion
{
    public class GraphQLLocalizationResolver : LocalizationResolver
    {
        private static readonly ConcurrentDictionary<string, object> KeyLocks = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Resolves a matching <see cref="ILocalization"/> for a given URL.
        /// </summary>
        /// <param name="url">The URL to resolve.</param>
        /// <returns>A <see cref="ILocalization"/> instance which base URL matches that of the given URL.</returns>
        /// <exception cref="DxaUnknownLocalizationException">If no matching Localization can be found.</exception>
        public override Localization ResolveLocalization(Uri url)
        {
            using (new Tracer(url))
            {
                Log.Trace($"Resolving localization for url: '{url}'");
                string urlLeftPart = url.GetLeftPart(UriPartial.Path);

                // TODO PERF: to optimize caching, we could only take the first part of the URL path (e.g. one or two levels)
                int espaceIndex = urlLeftPart.IndexOf("%");
                if (espaceIndex > 0)
                {
                    // TODO: This is a work-around for a bug in SDL Web 8 Publication Mapping: URLs with escaped characters don't resolve properly (CRQ-1585).
                    // Therefore we truncate the URL at the first escaped character for now (assuming that the URL is still specific enough to resolve the right Publication).
                    urlLeftPart = urlLeftPart.Substring(0, espaceIndex);
                }

                Localization result = GetCachedLocalization(urlLeftPart);
                if (result != null)
                {
                    return result;
                }

                lock (KeyLocks.GetOrAdd(urlLeftPart, new object()))
                {
                    result = GetCachedLocalization(urlLeftPart);
                    if (result != null)
                    {
                        RemoveLock(urlLeftPart);
                        return result;
                    }

                    // NOTE: we're not using UrlToLocalizationMapping here, because we may match too eagerly on a base URL when there is a matching mapping with a more specific URL.
                    PublicationMapping mapping = null;
                    try
                    {
                        mapping = ApiClientFactory.Instance.CreateClient().GetPublicationMapping(ContentNamespace.Sites, urlLeftPart);
                    }
                    catch
                    {
                        Log.Error($"Failed to get publication mapping for url {urlLeftPart}");
                    }

                    if (mapping == null || mapping.Port != url.Port.ToString()) // See CRQ-1195
                    {
                        RemoveLock(urlLeftPart);
                        throw new DxaUnknownLocalizationException($"No matching Localization found for URL '{urlLeftPart}'");
                    }

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


                    result.EnsureInitialized();

                    Log.Trace($"Localization for url '{url}' initialized and reports to be for Publication Id: {result.PublicationId()}, Path: {result.Path}");
                    CacheLocalization(urlLeftPart, result);                    
                    RemoveLock(urlLeftPart);
                    return result;
                }
            }
        }

        protected void CacheLocalization(String urlPart, Localization localization)
        {
            Log.Trace($"Attempting to cache localization for url part: '{urlPart}'");
            SiteConfiguration.CacheProvider.Store($"{urlPart}", CacheRegions.LocalizationResolving, localization);
        }

        protected Localization GetCachedLocalization(String urlPart)
        {
            Localization result;
            if (SiteConfiguration.CacheProvider.TryGet($"{urlPart}", CacheRegions.LocalizationResolving, out result))
            {
                Log.Trace($"Found cached localization for url part: '{urlPart}' with Publication Id: {result.PublicationId()}, Path: {result.Path}");
                return result;
            }
            Log.Trace($"No cached localization found for url part: '{urlPart}'");
            return null;
        }

        protected static void RemoveLock(String urlPart)
        {
            object tempKeyLock;
            KeyLocks.TryRemove(urlPart, out tempKeyLock);
        }

        public override Localization GetLocalization(string localizationId)
        {
            using (new Tracer(localizationId))
            {
                Localization result;
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
