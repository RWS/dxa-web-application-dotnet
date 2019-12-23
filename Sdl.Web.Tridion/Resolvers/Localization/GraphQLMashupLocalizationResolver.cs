using System;
using System.Text.RegularExpressions;
using Sdl.Tridion.Api.Client.Utils;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Tridion.Api.Client.ContentModel;
using Sdl.Web.Tridion.ApiClient;

namespace Sdl.Web.Tridion
{
    /// <summary>
    /// Localization Resolver to match against both Docs and Sites urls.
    /// </summary>
    public class GraphQLMashupLocalizationResolver : GraphQLLocalizationResolver
    {
        private static readonly Regex DocsPattern =
            new Regex(
                @"(^(?<pubId>\d+))|(^(?<pubId>\d+)/(?<itemId>\d+))|(^binary/(?<pubId>\d+)/(?<itemId>\d+))|(^api/binary/(?<pubId>\d+)/(?<itemId>\d+))|(^api/page/(?<pubId>\d+)/(?<pageId>\d+))|(^api/topic/(?<pubId>\d+)/(?<componentId>\d+)/(?<templateId>\d+))|(^api/toc/(?<pubId>\d+))|(^api/pageIdByReference/(?<pubId>\d+))|(^api/conditions/(?<pubId>\d+))|(^api/comments/(?<pubId>\d+)/(?<itemId>\d+))",
                RegexOptions.Compiled);
     
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
                return ResolveDocsLocalization(url) ?? base.ResolveLocalization(url);
            }
        }
      
        public override Localization GetLocalization(string localizationId)
        {
            using (new Tracer(localizationId))
            {
                Localization result;
                if (!KnownLocalizations.TryGetValue(localizationId, out result))
                {
                    // Check for namespace prefix (ish: or tcm:)
                    if (localizationId.StartsWith("ish:"))
                    {
                        CmUri uri = CmUri.FromString(localizationId);
                        result = new DocsLocalization(uri.PublicationId);
                    }
                    else if (localizationId.StartsWith("tcm:"))
                    {
                        CmUri uri = CmUri.FromString(localizationId);
                        result = new Localization { Id = uri.ItemId.ToString() };
                    }
                    else
                    {
                        // Attempt to resolve it from Docs
                        var client = ApiClientFactory.Instance.CreateClient();
                        Publication publication = client.GetPublication(ContentNamespace.Docs, int.Parse(localizationId), null, null);
                        result = publication != null ? new DocsLocalization(publication.PublicationId) : base.GetLocalization(localizationId);
                    }

                    KnownLocalizations.Add(localizationId, result);
                }

                return result;
            }
        }

        protected virtual Localization ResolveDocsLocalization(Uri url)
        {
            // Try if the URL looks like a Tridion Docs / DDWebApp URL
            string urlPath = url.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            if (string.IsNullOrEmpty(urlPath))
                return null; // No, it doesn't
            Match match = DocsPattern.Match(urlPath);
            if (!match.Success)
                return null; // No, it doesn't

            string localizationId = match.Groups["pubId"].Value;
            Localization result;
            if (!KnownLocalizations.TryGetValue(localizationId, out result))
            {
                result = new DocsLocalization { Id = localizationId };
                KnownLocalizations.Add(localizationId, result);
            }

            result.EnsureInitialized();
            return result;
        }
    }
}
