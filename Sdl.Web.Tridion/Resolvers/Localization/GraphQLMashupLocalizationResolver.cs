using System;
using System.Text.RegularExpressions;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.PublicContentApi.ContentModel;
using Sdl.Web.PublicContentApi.Utils;
using Sdl.Web.Tridion.PCAClient;
using Sdl.Web.Tridion.TridionDocs.Localization;

namespace Sdl.Web.Tridion
{
    public class GraphQLMashupLocalizationResolver : GraphQLLocalizationResolver
    {
        private static readonly Regex[] DocsPatterns = {
            new Regex(@"^(?<pubId>\d+)", RegexOptions.Compiled),
            new Regex(@"^(?<pubId>\d+)/(?<itemId>\d+)", RegexOptions.Compiled),
            new Regex(@"^binary/(?<pubId>\d+)/(?<itemId>\d+)", RegexOptions.Compiled),
            new Regex(@"^api/binary/(?<pubId>\d+)/(?<itemId>\d+)", RegexOptions.Compiled),
            new Regex(@"^api/page/(?<pubId>\d+)/(?<pageId>\d+)", RegexOptions.Compiled),
            new Regex(@"^api/topic/(?<pubId>\d+)/(?<componentId>\d+)/(?<templateId>\d+)", RegexOptions.Compiled),
            new Regex(@"^api/toc/(?<pubId>\d+)", RegexOptions.Compiled),
            new Regex(@"^api/pageIdByReference/(?<pubId>\d+)", RegexOptions.Compiled),
        };

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
                // Attempt to determine if we are looking at Docs content
                string urlPath = url.GetComponents(UriComponents.Path, UriFormat.Unescaped);
                if (!string.IsNullOrEmpty(urlPath))
                {
                    foreach (Regex t in DocsPatterns)
                    {
                        var match = t.Match(urlPath);
                        if (!match.Success) continue;
                        var localization = new DocsLocalization {Id = match.Groups["pubId"].Value};
                        return localization;
                    }
                }

                return base.ResolveLocalization(url);
            }
        }

        public override ILocalization GetLocalization(string localizationId)
        {
            using (new Tracer(localizationId))
            {
                ILocalization result;
                if (!KnownLocalizations.TryGetValue(localizationId, out result))
                {
                    // Check for namespace prefix (ish: or tcm:)
                    if (localizationId.StartsWith("ish:"))
                    {
                        CmUri uri = CmUri.FromString(localizationId);
                        return new DocsLocalization {Id = uri.PublicationId.ToString()};
                    }

                    if (localizationId.StartsWith("tcm:"))
                    {
                        CmUri uri = CmUri.FromString(localizationId);
                        return new Localization { Id = uri.PublicationId.ToString() };
                    }

                    // Attempt to resolve it from Docs
                    var client = PCAClientFactory.Instance.CreateClient();
                    var publication = client.GetPublication(ContentNamespace.Docs, int.Parse(localizationId), null, null);
                    if (publication != null)
                    {
                        return new DocsLocalization {Id = publication.Id};
                    }

                    return base.GetLocalization(localizationId);
                }

                return result;
            }
        }
    }
}
