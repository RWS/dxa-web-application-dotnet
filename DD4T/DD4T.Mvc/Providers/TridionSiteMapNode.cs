using System;
using System.Web;
using System.Collections;
using System.Collections.Specialized;
using DD4T.ContentModel;

namespace DD4T.Mvc.Providers
{
    public class TridionSiteMapNode : SiteMapNode
    {
        public static string NoUrlFoundPrefix = "/NoUrlInSitemap#";

        public TridionSiteMapNode(SiteMapProvider provider, string key, string uri, string url, string title, string description, IList roles, NameValueCollection attributes, NameValueCollection explicitResourceKeys, string implicitResourceKey) :
            base(provider, key, url, title, description, roles, attributes, explicitResourceKeys, implicitResourceKey)
        {
            if (url.StartsWith("tcm:"))
            {
                Url = MakeDummyUrl(url);
            }
            Uri = uri;
        }

        public override bool HasChildNodes
        {
            get
            {
                return ChildNodes.Count > 0;
            }
        }
        public override SiteMapNodeCollection ChildNodes
        {
            get
            {
                return base.ChildNodes;
            }
            set
            {
                base.ChildNodes = value;
            }
        }

        public new NameValueCollection Attributes
        {
            get
            {
                return base.Attributes;
            }
        }

        public override string Url
        {
            get
            {
                if (base.Url.StartsWith(NoUrlFoundPrefix))
                {
                    return string.Empty;
                }
                return base.Url;
            }
            set
            {
                base.Url = value;
            }

        }

        private string _resolvedUrl = null;
        public string ResolvedUrl
        {
            get
            {
                if (_resolvedUrl != null)
                    return _resolvedUrl;
                if (string.IsNullOrEmpty(Uri))
                {
                    _resolvedUrl = string.Empty;
                    return _resolvedUrl;
                }
                try
                {
                    TcmUri tcmUri = new TcmUri(Uri);
                    if (tcmUri.ItemTypeId == 16)
                    {
                        if (!String.IsNullOrEmpty(Uri))
                        {
                            string resolvedLink = ((TridionSiteMapProvider)this.Provider).LinkFactory.ResolveLink(Uri);
                            if (!String.IsNullOrEmpty(resolvedLink))
                            {
                                _resolvedUrl = resolvedLink;
                                return _resolvedUrl;
                            }
                        }
                        return null;
                    }
                    _resolvedUrl = Url;
                    return _resolvedUrl;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        public string Uri { get; set; }

        public int Level { get; set; }

        private string MakeDummyUrl(string inputUrl)
        {
            return NoUrlFoundPrefix + HttpUtility.HtmlEncode(inputUrl);
        }
    }
}
