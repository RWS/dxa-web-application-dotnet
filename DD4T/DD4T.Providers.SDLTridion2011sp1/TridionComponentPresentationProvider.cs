using System;
using System.Linq;
using T = Tridion.ContentDelivery.DynamicContent;
using Query = Tridion.ContentDelivery.DynamicContent.Query.Query;
using TMeta = Tridion.ContentDelivery.Meta;
using DD4T.ContentModel;
using System.Collections.Generic;
using DD4T.ContentModel.Contracts.Providers;
using System.Collections;
using DD4T.ContentModel.Querying;
using DD4T.Utils;
using DD4T.ContentModel.Contracts.Logging;

namespace DD4T.Providers.SDLTridion2011sp1
{
    /// <summary>
    /// 
    /// </summary>
    public class TridionComponentPresentationProvider : BaseProvider, IComponentPresentationProvider
    {

        Dictionary<int, T.ComponentPresentationFactory> _cpFactoryList = null;
        Dictionary<int, TMeta.ComponentMetaFactory> _cmFactoryList = null;

        private string selectByComponentTemplateId;
        private string selectByOutputFormat;

        public TridionComponentPresentationProvider(IProvidersCommonServices commonServices)
            : base(commonServices)
        {
            selectByComponentTemplateId = Configuration.SelectComponentByComponentTemplateId;
            selectByOutputFormat = Configuration.SelectComponentByOutputFormat;
            _cpFactoryList = new Dictionary<int, T.ComponentPresentationFactory>();
            _cmFactoryList = new Dictionary<int, TMeta.ComponentMetaFactory>();
        }

        #region IComponentPresentationProvider
        public string GetContent(string uri, string templateUri = "")
        {
            LoggerService.Debug(">>GetContent({0})", LoggingCategory.Performance, uri);

            TcmUri tcmUri = new TcmUri(uri);

            TcmUri templateTcmUri = new TcmUri(templateUri);

            T.ComponentPresentationFactory cpFactory = GetComponentPresentationFactory(tcmUri.PublicationId);

            T.ComponentPresentation cp = null;

            if (!String.IsNullOrEmpty(templateUri))
            {
                cp = cpFactory.GetComponentPresentation(tcmUri.ItemId, templateTcmUri.ItemId);
                if (cp != null)
                    return cp.Content;
            }

            if (!string.IsNullOrEmpty(selectByComponentTemplateId))
            {
                cp = cpFactory.GetComponentPresentation(tcmUri.ItemId, Convert.ToInt32(selectByComponentTemplateId));
                if (cp != null)
                {
                    LoggerService.Debug("<<GetContent({0}) - by ct id", LoggingCategory.Performance, uri);
                    return cp.Content;
                }
            }
            if (!string.IsNullOrEmpty(selectByOutputFormat))
            {
                cp = cpFactory.GetComponentPresentationWithOutputFormat(tcmUri.ItemId, selectByOutputFormat);
                if (cp != null)
                {
                    LoggerService.Debug("<<GetContent({0}) - by output format", LoggingCategory.Performance, uri);
                    return cp.Content;
                }
            }

            LoggerService.Debug("GetContent: about to get component presentations with Highst Priority for {0}", LoggingCategory.Performance, tcmUri.ToString());
            cp = cpFactory.GetComponentPresentationWithHighestPriority(tcmUri.ItemId);
            LoggerService.Debug("GetContent: get component presentations with Highst Priority for {0}", LoggingCategory.Performance, tcmUri.ToString());
            if (cp != null)
                return cp.Content;
            //foreach (Tridion.ContentDelivery.DynamicContent.ComponentPresentation _cp in cps)
            //{
            //    if (_cp != null)
            //    {
            //        LoggerService.Debug("<<GetContent({0}) - find all", LoggingCategory.Performance, uri);
            //        return _cp.Content;
            //    }
            //}

            LoggerService.Debug("<<GetContent({0}) - not found", LoggingCategory.Performance, uri);
            return string.Empty;
        }

        /// <summary>
        /// Returns the Component contents which could be found. Components that couldn't be found don't appear in the list. 
        /// </summary>
        /// <param name="componentUris"></param>
        /// <returns></returns>
        public List<string> GetContentMultiple(string[] componentUris)
        {
            //            TcmUri uri = new TcmUri(componentUris.First());
            var components =
                componentUris
                .Select(componentUri => { TcmUri uri = new TcmUri(componentUri); return (T.ComponentPresentation)GetComponentPresentationFactory(uri.PublicationId).FindAllComponentPresentations(componentUri)[0]; })
                .Where(cp => cp != null)
                .Select(cp => cp.Content)
                .ToList();

            return components;

        }

        public IList<string> FindComponents(IQuery query)
        {
            if (!(query is ITridionQueryWrapper))
                throw new InvalidCastException("Cannot execute query because it is not based on " + typeof(ITridionQueryWrapper).Name);

            Query tridionQuery = ((ITridionQueryWrapper)query).ToTridionQuery();
            return tridionQuery.ExecuteQuery();
        }


        public DateTime GetLastPublishedDate(string uri)
        {
            TcmUri tcmUri = new TcmUri(uri);
            TMeta.IComponentMeta cmeta = GetComponentMetaFactory(tcmUri.PublicationId).GetMeta(tcmUri.ItemId);
            return cmeta == null ? DateTime.Now : cmeta.LastPublicationDate;
        }
        #endregion

        #region private
        private object lock1 = new object();
        private object lock2 = new object();
        private TMeta.ComponentMetaFactory GetComponentMetaFactory(int publicationId)
        {
            if (_cmFactoryList.ContainsKey(publicationId))
                return _cmFactoryList[publicationId];

            lock (lock1)
            {
                if (!_cmFactoryList.ContainsKey(publicationId)) // we must test again, because in the mean time another thread might have added a record to the dictionary!
                {
                    _cmFactoryList.Add(publicationId, new TMeta.ComponentMetaFactory(publicationId));
                }
            }
            return _cmFactoryList[publicationId];
        }
        private T.ComponentPresentationFactory GetComponentPresentationFactory(int publicationId)
        {
            if (_cpFactoryList.ContainsKey(publicationId))
                return _cpFactoryList[publicationId];

            lock (lock2)
            {
                if (!_cpFactoryList.ContainsKey(publicationId)) // we must test again, because in the mean time another thread might have added a record to the dictionary!
                {
                    _cpFactoryList.Add(publicationId, new T.ComponentPresentationFactory(publicationId));
                }
            }
            return _cpFactoryList[publicationId];
        }
        #endregion
    }
}
