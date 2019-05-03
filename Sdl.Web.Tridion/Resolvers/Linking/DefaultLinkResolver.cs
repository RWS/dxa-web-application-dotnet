using Sdl.Tridion.Api.Client.Utils;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Tridion.ContentManager;
using Tridion.ContentDelivery.Web.Linking;
using CmUri = Sdl.Tridion.Api.Client.Utils.CmUri;

namespace Sdl.Web.Tridion.Linking
{
    /// <summary>
    /// Default Link Resolver implementation
    /// </summary>
    public class DefaultLinkResolver : ILinkResolver, ILinkResolver2
    {
        #region ILinkResolver Members

        /// <summary>
        /// Resolves a link URI (TCM URI or site URL) to a normalized site URL.
        /// </summary>
        /// <param name="sourceUri">The source URI (TCM URI or site URL)</param>
        /// <param name="resolveToBinary">Specifies whether a link to a Multimedia Component should be resolved directly to its Binary (<c>true</c>) or as a regular Component link.</param>
        /// <param name="localization">The context Localization (optional, since the TCM URI already contains a Publication ID, but this allows resolving in a different context).</param>
        /// <returns>The resolved URL.</returns>
        public string ResolveLink(string sourceUri, bool resolveToBinary = false, Localization localization = null) 
            => ResolveLink(sourceUri, resolveToBinary, localization, null);
        #endregion

        #region ILinkResolver2 Members
        public string ResolveLink(string sourceUri, bool resolveToBinary = false, Localization localization = null,
            string contextId = null)
        {
            if (sourceUri == null)
            {
                return null;
            }

            string url;
            if (sourceUri.IsCmIdentifier())
            {
                Tridion.ContentManager.TcmUri tcmUri = new Tridion.ContentManager.TcmUri(sourceUri);
                url = ResolveLink(tcmUri, resolveToBinary, localization, contextId);
            }
            else
            {
                url = sourceUri;
            }

            // Strip off default extension / page name
            if (url != null && url.EndsWith(Constants.DefaultExtension))
            {
                url = url.Substring(0, url.Length - Constants.DefaultExtension.Length);
                if (url.EndsWith("/" + Constants.DefaultExtensionLessPageName))
                {
                    url = url.Substring(0, url.Length - Constants.DefaultExtensionLessPageName.Length);
                }
            }
            return url;
        }
        #endregion

        private static string ResolveLink(Tridion.ContentManager.TcmUri tcmUri, bool resolveToBinary, Localization localization, string contextId)
        {           
            switch (tcmUri.ItemType)
            {
                case ItemType.Page:
                    return ResolvePageLink(tcmUri, localization);

                case ItemType.Component:
                    // If requested (resolveToBinary = true), try to resolve Binary Link first.
                    string binaryLink = null;
                    if (resolveToBinary)
                    {
                        binaryLink = ResolveBinaryLink(tcmUri, localization);
                    }
                    if (binaryLink != null)
                        return binaryLink;

                    int pageId;
                    CmUri cmUri;
                    if (CmUri.TryParse(contextId, out cmUri))
                    {
                        pageId = cmUri.ItemId;
                    }
                    else if (!int.TryParse(contextId, out pageId))
                    {
                        pageId = -1;
                    }

                    return ResolveComponentLink(tcmUri, localization, pageId);

                default:
                    throw new DxaException("Unexpected item type in TCM URI: " + tcmUri);
            }
        }

        private static string GetPublicationUri(Tridion.ContentManager.TcmUri tcmUri, Localization localization)
            => (localization == null) ? $"tcm:0-{tcmUri.PublicationId}-1" : localization.GetCmUri();

        private static string ResolveComponentLink(Tridion.ContentManager.TcmUri tcmUri, Localization localization, int pageId)
        {
            ComponentLink linker = new ComponentLink(GetPublicationUri(tcmUri, localization));
            Link link = linker.GetLink(pageId, tcmUri.ItemId, -1, null, "", false, false);
            return link.IsResolved ? link.Url : null;
        }

        private static string ResolveBinaryLink(Tridion.ContentManager.TcmUri tcmUri, Localization localization)
        {
            BinaryLink linker = new BinaryLink(GetPublicationUri(tcmUri, localization));
            Link link = linker.GetLink(tcmUri.ToString(), null, null, null, false);
            return link.IsResolved ? link.Url : null;
        }

        private static string ResolvePageLink(Tridion.ContentManager.TcmUri tcmUri, Localization localization)
        {
            PageLink linker = new PageLink(GetPublicationUri(tcmUri, localization));
            Link link = linker.GetLink(tcmUri.ItemId);
            return link.IsResolved ? link.Url : null;
        }       
    }
}
