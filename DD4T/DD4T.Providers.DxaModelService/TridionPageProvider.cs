using System;
using Tridion.ContentDelivery.DynamicContent;
using Tridion.ContentDelivery.DynamicContent.Query;
using Query = Tridion.ContentDelivery.DynamicContent.Query.Query;
using Tridion.ContentDelivery.Meta;
using System.Collections.Generic;
using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Contracts.Logging;
using Sdl.Web.ModelService.Request;

namespace DD4T.Providers.DxaModelService
{  
    public class TridionPageProvider : BaseProvider, IPageProvider
    {
        public TridionPageProvider(IProvidersCommonServices providersCommonServices)
            : base(providersCommonServices)
        { }

        #region IPageProvider Members

        /// <summary>
        /// Get all urls of published pages
        /// </summary>
        /// <param name="includeExtensions"></param>
        /// <param name="pathStarts"></param>
        /// <param name="publicationID"></param>
        /// <returns></returns>
        public string[] GetAllPublishedPageUrls(string[] includeExtensions, string[] pathStarts)
        {
            ItemTypeCriteria isPage = new ItemTypeCriteria(64);  // TODO There must be an enum of these somewhere
            PublicationCriteria currentPublication = new PublicationCriteria(PublicationId); //Todo: add logic to determine site on url

            Criteria pageInPublication = CriteriaFactory.And(isPage, currentPublication);

            if (includeExtensions.Length > 0)
            {
                PageURLCriteria[] extensionsCriteria = new PageURLCriteria[includeExtensions.Length];
                int criteriaCount = 0;
                foreach (string pageExtension in includeExtensions)
                {
                    extensionsCriteria.SetValue(new PageURLCriteria("%" + pageExtension, Criteria.Like), criteriaCount);
                    criteriaCount++;
                }

                Criteria allExtensions = CriteriaFactory.Or(extensionsCriteria);
                pageInPublication = CriteriaFactory.And(pageInPublication, allExtensions);
            }

            if (pathStarts.Length > 0)
            {
                PageURLCriteria[] pathCriteria = new PageURLCriteria[pathStarts.Length];
                int criteriaCount = 0;
                foreach (string requiredPath in pathStarts)
                {
                    pathCriteria.SetValue(new PageURLCriteria(requiredPath + "%", Criteria.Like), criteriaCount);
                    criteriaCount++;
                }

                Criteria allPaths = CriteriaFactory.Or(pathCriteria);
                pageInPublication = CriteriaFactory.And(pageInPublication, allPaths);
            }

            Query findPages = new Query(pageInPublication);
            string[] pageUris = findPages.ExecuteQuery();

            // Need to get PageMeta data to find all the urls
            List<string> pageUrls = new List<string>();
            foreach (string uri in pageUris)
            {
                TcmUri tcmUri = new TcmUri(uri);
                PageMetaFactory metaFactory = GetPageMetaFactory(tcmUri.PublicationId);
                IPageMeta currentMeta = metaFactory.GetMeta(uri);
                pageUrls.Add(currentMeta.UrlPath);
            }
            return pageUrls.ToArray();
        }

        /// <summary>
        /// Gets the raw string (xml) from the broker db by URL
        /// </summary>
        /// <param name="Url">URL of the page</param>
        /// <returns>String with page xml or empty string if no page was found</returns>
        public string GetContentByUrl(string url)
        {
            LoggerService.Debug(">>GetContentByUrl({0})", LoggingCategory.Performance, url);
            PageModelRequest req = new PageModelRequest
            {
                PublicationId = PublicationId,
                ContentType = ContentType.RAW,
                DataModelType = DataModelType.DD4T,
                PageInclusion = PageInclusion.INCLUDE,
                Path = url
            };
            return ModelServiceClient.PerformRequest(req).Response;
        }

        /// <summary>
        /// Gets the raw string (xml) from the broker db by URI
        /// </summary>
        /// <param name="Url">TCM URI of the page</param>
        /// <returns>String with page xml or empty string if no page was found</returns>
        public string GetContentByUri(string tcmUri)
        {
            TcmUri tcm = new TcmUri(tcmUri);
            PageMetaFactory metaFactory = GetPageMetaFactory(tcm.PublicationId);
            return GetContentByUrl(metaFactory.GetMeta(tcm.ItemId).UrlPath);
        }

        public DateTime GetLastPublishedDateByUrl(string url)
        {
            int pubId = PublicationId;
            LoggerService.Debug("GetLastPublishedDateByUrl found publication id {0}, url = {1}", pubId, url);
            using (var pMetaFactory = new PageMetaFactory(pubId))
            {
                IPageMeta pageInfo = pMetaFactory.GetMetaByUrl(pubId, url); // TODO: Temporarily removed using statement, because IPageMeta is not IDisposable (yet) in CDaaS build #422
                {
                    return pageInfo?.LastPublicationDate ?? DateTime.Now;
                }
            }
        }

        public DateTime GetLastPublishedDateByUri(string uri)
        {
            TcmUri tcmUri = new TcmUri(uri);
            PageMetaFactory pMetaFactory = GetPageMetaFactory(tcmUri.PublicationId);
            var pageInfo = pMetaFactory.GetMeta(uri);
            return pageInfo?.LastPublicationDate ?? DateTime.Now;
        }

        private readonly object lock1 = new object();
        private readonly Dictionary<int, PageMetaFactory> _pmFactoryList = new Dictionary<int, PageMetaFactory>();
        private PageMetaFactory GetPageMetaFactory(int publicationId)
        {
            if (_pmFactoryList.ContainsKey(publicationId))
                return _pmFactoryList[publicationId];

            lock (lock1)
            {
                if (!_pmFactoryList.ContainsKey(publicationId)) // we must test again, because in the mean time another thread might have added a record to the dictionary!
                {
                    _pmFactoryList.Add(publicationId, new PageMetaFactory(publicationId));
                }
            }
            return _pmFactoryList[publicationId];
        }
        #endregion
    }
}
