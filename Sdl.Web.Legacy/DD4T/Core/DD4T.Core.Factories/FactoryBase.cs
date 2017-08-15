using System;
using DD4T.ContentModel.Contracts.Caching;
using DD4T.Utils;
using DD4T.ContentModel.Contracts.Resolvers;
using DD4T.ContentModel.Contracts.Serializing;
using DD4T.ContentModel;
using DD4T.Serialization;
using DD4T.ContentModel.Contracts.Configuration;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.ContentModel.Factories;

namespace DD4T.Factories
{
    /// <summary>
    /// Base class for all factories
    /// </summary>
    public abstract class FactoryBase
    {

        public IPublicationResolver PublicationResolver { get; set; }
        public ICacheAgent CacheAgent { get; set; }

        protected readonly IDD4TConfiguration Configuration;
        protected readonly ILogger LoggerService;


        public FactoryBase(IFactoryCommonServices factoryCommonServices)
        {
            if (factoryCommonServices == null)
                throw new ArgumentNullException("factoryCommonServices");

            
            LoggerService = factoryCommonServices.Logger;
            PublicationResolver = factoryCommonServices.PublicationResolver;
            Configuration = factoryCommonServices.Configuration;
            CacheAgent = factoryCommonServices.CacheAgent;
        }

        /// <summary>
        /// Abstract method to be overridden by each implementation. The method should return the DateTime when the object in the cache was last published.
        /// </summary>
        /// <param name="key">Key of the object in the cache</param>
        /// <param name="cachedItem">The object in the cache</param>
        /// <returns></returns>
        [Obsolete]
        public abstract DateTime GetLastPublishedDateCallBack(string key, object cachedItem);


        #region publication resolving
        private int? _publicationId = null;

        /// <summary>
        /// Returns the current publicationId
        /// </summary>  
        protected virtual int PublicationId 
        {
            get
            {
                if (_publicationId == null)
                    return PublicationResolver.ResolvePublicationId();
                return _publicationId.Value;
            }
            set
            {
                _publicationId = value;
            }
        }
        #endregion

        #region private properties
        protected bool IncludeLastPublishedDate
        {
            get
            {
                return Configuration.IncludeLastPublishedDate;
            }
        }
        #endregion

        // inner class which acts as a placeholder for the real serializer service (Xml or Json)
        internal class AutoDetectSerializerService : ISerializerService
        {
            public T Deserialize<T>(string input) where T : IModel
            {
                return SerializerServiceFactory.FindSerializerServiceForContent(input).Deserialize<T>(input);
            }

            public bool IsAvailable()
            {
                return true;
            }

            public string Serialize<T>(T input) where T : IModel
            {
                // in autodetect mode, no serialization should occur, this is only to consume mixed data formats, not to produce them!
                throw new NotImplementedException();
            }
        }
    }
}
