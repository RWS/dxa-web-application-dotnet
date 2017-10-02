using DD4T.Utils;

namespace DD4T.Providers.DxaModelService
{
    using System;
    using System.Collections.Generic;
    using Tridion.ContentDelivery.Web.Linking;
    using ContentModel.Contracts.Providers;
    using Utils.ExtensionMethods;

    public class TridionLinkProvider : BaseProvider, ILinkProvider, IDisposable
    {
        public TridionLinkProvider(IProvidersCommonServices providersCommonServices)
            : base(providersCommonServices)
        { }

        private ComponentLink _componentLink = null;
        private static readonly TcmUri EmptyTcmUri = new TcmUri("tcm:0-0-0");

        public ComponentLink ComponentLink => _componentLink ?? (_componentLink = new ComponentLink(PublicationId));
        private readonly Dictionary<int, ComponentLink> _componentLinks = new Dictionary<int, ComponentLink>();
        private readonly object lock1 = new object();
        protected ComponentLink GetComponentLink(TcmUri uri)
        {
            if (_componentLinks.ContainsKey(uri.PublicationId))
                return _componentLinks[uri.PublicationId];

            lock (lock1)
            {
                if (!_componentLinks.ContainsKey(uri.PublicationId)) // we must test again, because in the mean time another thread might have added a record to the dictionary!
                {
                    _componentLinks.Add(uri.PublicationId, new ComponentLink(uri.PublicationId));
                }
            }
            return _componentLinks[uri.PublicationId];
        }

        public string ResolveLink(string componentUri) => ResolveLink(TcmUri.NullUri.ToString(), componentUri, TcmUri.NullUri.ToString());

        public virtual string ResolveLink(string sourcePageUri, string componentUri, string excludeComponentTemplateUri)
        {
            TcmUri componentUriToLinkTo = new TcmUri(componentUri);
            TcmUri pageUri = new TcmUri(sourcePageUri);
            TcmUri componentTemplateUri = new TcmUri(excludeComponentTemplateUri);
            var linkToAnchor = Configuration.LinkToAnchor;

            if (!componentUriToLinkTo.Equals(EmptyTcmUri))
            {
                Link link = GetComponentLink(componentUriToLinkTo).GetLink(pageUri.ToString(), componentUriToLinkTo.ToString(), componentTemplateUri.ToString(), String.Empty, String.Empty, false, linkToAnchor);
                if (!link.IsResolved)
                {
                    return null;
                }

                return linkToAnchor && link.Anchor != "0" ?
                    $"{link.Url}#{Configuration.UseUriAsAnchor.GetLocalAnchorTag(componentUriToLinkTo, link.Anchor)}"
                    : link.Url;
            }

            return null;
        }

        #region IDisposable
        protected virtual void Dispose(bool isDisposed)
        {
            if (isDisposed) return;
            if (_componentLink != null)
            {
                _componentLink.Dispose();
                _componentLink = null;
            }
            foreach (ComponentLink cl in _componentLinks.Values)
            {
                cl?.Dispose();
            }
            _componentLinks.Clear();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

