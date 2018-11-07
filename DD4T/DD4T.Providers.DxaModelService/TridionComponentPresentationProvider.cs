using System;
using System.Linq;
using T = Tridion.ContentDelivery.DynamicContent;
using Query = Tridion.ContentDelivery.DynamicContent.Query.Query;
using TMeta = Tridion.ContentDelivery.Meta;
using System.Collections.Generic;
using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Querying;
using DD4T.ContentModel.Contracts.Logging;
using Sdl.Web.ModelService.Request;

namespace DD4T.Providers.DxaModelService
{
    public class TridionComponentPresentationProvider : BaseProvider, IComponentPresentationProvider
    {
        private readonly Dictionary<int,T.ComponentPresentationFactory> _cpFactoryList;
        private readonly Dictionary<int,TMeta.ComponentMetaFactory> _cmFactoryList;
        private string _selectByComponentTemplateId;
        private string _selectByOutputFormat;

        public TridionComponentPresentationProvider(IProvidersCommonServices providersCommonServices)
            : base(providersCommonServices)
        {
#pragma warning disable 618
            _selectByComponentTemplateId = Configuration.SelectComponentByComponentTemplateId;
            _selectByOutputFormat = Configuration.SelectComponentByOutputFormat;
#pragma warning restore 618
            _cpFactoryList = new Dictionary<int, T.ComponentPresentationFactory>();
            _cmFactoryList = new Dictionary<int,TMeta.ComponentMetaFactory>();
        }

        #region IComponentProvider

        public string GetContent(string uri, string templateUri = "")
        {
            LoggerService.Debug(">>DD4T.Providers.DxaModelService::GetContent({0})", LoggingCategory.Performance, uri);
            LoggerService.Debug(">>DD4T.Providers.DxaModelService::GetContent({0})", LoggingCategory.Performance, uri);
            TcmUri tcmUri = new TcmUri(uri);
            TcmUri templateTcmUri = new TcmUri(templateUri);
            EntityModelRequest request = new EntityModelRequest
            {
                PublicationId = PublicationId,
                DataModelType = DataModelType.DD4T,
                DcpType = DcpType.HIGHEST_PRIORITY,
                ComponentId = tcmUri.ItemId
            };

            if (!string.IsNullOrEmpty(templateUri))
            {
                request.TemplateId = templateTcmUri.ItemId;
            }

            try
            {
                var response = ModelServiceClient.PerformRequest<IDictionary<string, object>>(request);
                return response.Response["Content"] as string;
            }
            catch
            {
            }
            return null;
        }

        /// <summary>
        /// Returns the Component contents which could be found. Components that couldn't be found don't appear in the list. 
        /// </summary>
        /// <param name="componentUris"></param>
        /// <returns></returns>
        public List<string> GetContentMultiple(string[] componentUris)
        {
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
            if (! (query is ITridionQueryWrapper))
                throw new InvalidCastException("Cannot execute query because it is not based on " + typeof(ITridionQueryWrapper).Name);

            Query tridionQuery = ((ITridionQueryWrapper)query).ToTridionQuery();
            return tridionQuery.ExecuteQuery();
        }

        public DateTime GetLastPublishedDate(string uri)
        {
            TcmUri tcmUri = new TcmUri(uri);
            TMeta.IComponentMeta cmeta = GetComponentMetaFactory(tcmUri.PublicationId).GetMeta(tcmUri.ItemId);
            return cmeta?.LastPublicationDate ?? DateTime.Now;
        }
        #endregion

        #region private
        private readonly object lock1 = new object();
        private readonly object lock2 = new object();
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
