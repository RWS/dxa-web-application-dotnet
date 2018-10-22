using System;
using System.Text.RegularExpressions;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.PublicContentApi.ContentModel;
using Sdl.Web.PublicContentApi.Utils;
using Sdl.Web.Tridion.PCAClient;

namespace Sdl.Web.Tridion
{
    /// <summary>
    /// Localization Resolver to match against both Docs and Sites urls.
    /// </summary>
    public class GraphQLMashupLocalizationResolver : GraphQLLocalizationResolver
    {
        private static readonly Regex DocsPattern =
            new Regex(
                @"(^(?<pubId>\d+))|(^(?<pubId>\d+)/(?<itemId>\d+))|(^binary/(?<pubId>\d+)/(?<itemId>\d+))|(^api/binary/(?<pubId>\d+)/(?<itemId>\d+))|(^api/page/(?<pubId>\d+)/(?<pageId>\d+))|(^api/topic/(?<pubId>\d+)/(?<componentId>\d+)/(?<templateId>\d+))|(^api/toc/(?<pubId>\d+))|(^api/pageIdByReference/(?<pubId>\d+))",
                RegexOptions.Compiled);
     
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
                return ResolveDocsLocalization(url) ?? base.ResolveLocalization(url);
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
                    return publication != null ? new DocsLocalization {Id = publication.PublicationId.ToString()} : base.GetLocalization(localizationId);
                }

                return result;
            }
        }      

        protected virtual ILocalization ResolveDocsLocalization(Uri url)
        {
            var urlPath = url.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            if (string.IsNullOrEmpty(urlPath)) return null;
            var match = DocsPattern.Match(urlPath);
            return !match.Success ? null : new DocsLocalization { Id = match.Groups["pubId"].Value };
        }
    }
}
