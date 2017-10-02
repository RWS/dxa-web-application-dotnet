using System;
using System.Collections.Generic;
using DD4T.ContentModel;
using DD4T.ContentModel.Contracts.Providers;
using DD4T.Utils;
using DD4T.ContentModel.Factories;
using DD4T.ContentModel.Contracts.Resolvers;
using DD4T.ContentModel.Contracts.Configuration;

namespace DD4T.Factories
{

    public class LinkFactory : FactoryBase, ILinkFactory
    {
        const string CacheKeyFormat = "Link_{0}";
        const string CacheKeyFormatExtended = "Link_{0}_{1}_{2}";
        const string CacheValueNull = "UnresolvedLink";

        public const string CacheRegion = "Link";

        //private const string uriPrefix = "tcm:";
        private static TcmUri emptyTcmUri = new TcmUri("tcm:0-0-0");
        private Dictionary<int, ILinkProvider> _linkProviders = new Dictionary<int, ILinkProvider>();

        public ILinkProvider LinkProvider { get; set; }

        public LinkFactory(ILinkProvider linkProvider, IFactoryCommonServices factoryCommonServices)
            : base(factoryCommonServices)
        {
            if (linkProvider == null)
                throw new ArgumentNullException("linkProvier");

            LinkProvider = linkProvider;
        }

        //private object lock1 = new object();
        //private ILinkProvider GetLinkProvider(string uri)
        //{
        //    TcmUri u = new TcmUri(uri);
        //    if (u == null)
        //        // invalid uri, return null
        //        return null;

        //    if (_linkProviders.ContainsKey(u.PublicationId))
        //        return _linkProviders[u.PublicationId];
        //    lock (lock1)
        //    {
        //        if (!_linkProviders.ContainsKey(u.PublicationId)) // we must test again, because in the mean time another thread might have added a record to the dictionary!
        //        {
        //            Type t = LinkProvider.GetType();
        //            ILinkProvider lp = (ILinkProvider)Activator.CreateInstance(t);
        //            lp.PublicationId = u.PublicationId;
        //            _linkProviders.Add(u.PublicationId, lp);
        //        }
        //    }
        //    return _linkProviders[u.PublicationId];
        //}

        public string ResolveLink(string componentUri)
        {
            string cacheKey = String.Format(CacheKeyFormat, componentUri);
            string link = (string)CacheAgent.Load(cacheKey);
            if (link != null)
            {
                if (link.Equals(CacheValueNull))
                {
                    return null;
                }
                return link;
            }
            else
            {
               
                string resolvedUrl = LinkProvider.ResolveLink(componentUri);
                if (resolvedUrl == null)
                {
                    //CacheAgent.Store(cacheKey, CacheRegion, CacheValueNull, new List<string>() { String.Format(ComponentFactory.CacheKeyFormatByUri, componentUri) });
                    CacheAgent.Store(cacheKey, CacheRegion, CacheValueNull);
                }
                else
                {
                    //CacheAgent.Store(cacheKey, CacheRegion, resolvedUrl, new List<string>() { String.Format(ComponentFactory.CacheKeyFormatByUri, componentUri) });
                    CacheAgent.Store(cacheKey, CacheRegion, resolvedUrl);
                }
                return resolvedUrl;
            }
        }

        public string ResolveLink(string sourcePageUri, string componentUri, string excludeComponentTemplateUri)
        {
            string cacheKey = String.Format(CacheKeyFormatExtended, sourcePageUri, componentUri, excludeComponentTemplateUri);
            string link = (string)CacheAgent.Load(cacheKey);
            if (link != null)
            {
                if (link.Equals(CacheValueNull))
                {
                    return null;
                }
                return link;
            }
            else
            {
                string resolvedUrl = LinkProvider.ResolveLink(sourcePageUri, componentUri, excludeComponentTemplateUri);
                if (resolvedUrl == null)
                {
                    CacheAgent.Store(cacheKey, CacheRegion, CacheValueNull);
                }
                else
                {
                    CacheAgent.Store(cacheKey, CacheRegion, resolvedUrl);
                }
                return resolvedUrl;
            }
        }
        [Obsolete]
        public override DateTime GetLastPublishedDateCallBack(string key, object cachedItem)
        {
            throw new NotImplementedException();
        }
    }
}

