using System;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Tridion.ContentManager;
using Tridion.ContentDelivery.Web.Linking;

namespace Sdl.Web.Tridion.Linking
{
    /// <summary>
    /// Default/Tridion Link Resolver implementation
    /// </summary>
    public class TridionLinkResolver : ILinkResolver
    {
        #region ILinkResolver Members

        /// <summary>
        /// Resolves a link URI (TCM URI or site URL) to a normalized site URL.
        /// </summary>
        /// <param name="sourceUri">The source URI (TCM URI or site URL)</param>
        /// <returns>The resolved URL.</returns>
        public string ResolveLink(string sourceUri)
        {
            return ResolveLink(sourceUri, 0);
        }

        /// <summary>
        /// Resolves a link URI (TCM URI or site URL) to a normalized site URL in context of a given Localization.
        /// </summary>
        /// <param name="sourceUri">The source URI (TCM URI or site URL)</param>
        /// <param name="localizationId">The Localization ID.</param>
        /// <returns>The resolved URL.</returns>
        public string ResolveLink(string sourceUri, int localizationId)
        {
            if (sourceUri == null)
            {
                return null;
            }

            string url;
            if (sourceUri.StartsWith("tcm:"))
            {
                TcmUri tcmUri = new TcmUri(sourceUri);
                url = ResolveLink(tcmUri, localizationId, false);
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
        
        private static string ResolveLink(TcmUri tcmUri, int localizationId, bool isBinary)
        {
            switch (tcmUri.ItemType)
            {
                case ItemType.Page:
                    return ResolvePageLink(tcmUri, localizationId);
                case ItemType.Component:
                    return isBinary ? ResolveBinaryLink(tcmUri, localizationId) : ResolveComponentLink(tcmUri, localizationId);
                default:
                    throw new DxaException("Unexpected item type in TCM URI: " + tcmUri);
            }
        }

        private static string ResolveComponentLink(TcmUri tcmUri, int localizationId = 0)
        {
            int publicationId = localizationId == 0 ? tcmUri.PublicationId : localizationId;
            ComponentLink linker = new ComponentLink(publicationId);
            Link link = linker.GetLink(tcmUri.ItemId);
            return link.IsResolved ? link.Url : null;
        }

        private static string ResolveBinaryLink(TcmUri tcmUri, int localizationId = 0)
        {
            int publicationId = localizationId == 0 ? tcmUri.PublicationId : localizationId;
            BinaryLink linker = new BinaryLink(publicationId);
            Link link = linker.GetLink(tcmUri, null, null, null, false);
            return link.IsResolved ? link.Url : null;
        }

        private static string ResolvePageLink(TcmUri tcmUri, int localizationId = 0)
        {
            int publicationId = localizationId == 0 ? tcmUri.PublicationId : localizationId;
            PageLink linker = new PageLink(publicationId);
            Link link = linker.GetLink(tcmUri.ItemId);
            return link.IsResolved ? link.Url : null;
        }
    }
}
