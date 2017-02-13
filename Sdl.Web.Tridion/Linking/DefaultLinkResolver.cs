using System;
using Sdl.Web.Common;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Tridion.ContentManager;
using Tridion.ContentDelivery.Web.Linking;

namespace Sdl.Web.Tridion.Linking
{
    /// <summary>
    /// Default Link Resolver implementation
    /// </summary>
    public class DefaultLinkResolver : ILinkResolver
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
        {
            if (sourceUri == null)
            {
                return null;
            }

            string url;
            if (sourceUri.IsCmIdentifier())
            {
                TcmUri tcmUri = new TcmUri(sourceUri);
                url = ResolveLink(tcmUri, resolveToBinary, localization);
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

        private static string ResolveLink(TcmUri tcmUri, bool resolveToBinary, Localization localization)
        {
            int localizationId = (localization == null) ? 0 : Convert.ToInt32(localization.LocalizationId);
            switch ((ItemType)tcmUri.ItemTypeId)
            {
                case ItemType.Page:
                    return ResolvePageLink(tcmUri, localizationId);

                case ItemType.Component:
                    // If requested (resolveToBinary = true), try to resolve Binary Link first.
                    string binaryLink = null;
                    if (resolveToBinary)
                    {
                        binaryLink = ResolveBinaryLink(tcmUri, localizationId);
                    }
                    return binaryLink ?? ResolveComponentLink(tcmUri, localizationId);

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
            Link link = linker.GetLink(tcmUri.ToString(), null, null, null, false);
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
