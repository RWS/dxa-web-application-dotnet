using System;
using System.Collections.Generic;
using System.Linq;
using DD4T.ContentModel;
using DD4T.ContentModel.Contracts.Caching;
using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Exceptions;
using DD4T.ContentModel.Querying;
using DD4T.Utils;
using DD4T.ContentModel.Factories;
using DD4T.ContentModel.Contracts.Serializing;
using DD4T.Serialization;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.Utils.Caching;

namespace DD4T.Factories
{
    /// <summary>
    /// Factory for the creation of IComponents
    /// </summary>
    public class ComponentPresentationFactory : FactoryBase, IComponentPresentationFactory
    {
        public const string CacheRegion = "ComponentPresentation";
        public IComponentPresentationProvider ComponentPresentationProvider { get; set; }

        public ComponentPresentationFactory(IComponentPresentationProvider componentPresentationProvider, IFactoryCommonServices factoryCommonServices)
            : base(factoryCommonServices)
        {
            if (componentPresentationProvider == null)
                throw new ArgumentNullException("componentPresentationProvider");

            ComponentPresentationProvider = componentPresentationProvider;
            //overriding cacheAgent GetLastPublished property
            //CacheAgent.GetLastPublishDateCallBack = GetLastPublishedDateCallBack;
        }

        private string DataFormat
        {
            get
            {
                return Configuration.DataFormat;
            }
        }

        private ISerializerService _serializerService;
        private ISerializerService SerializerService
        {
            get
            {
                if (_serializerService == null)
                {
                    if (DataFormat.ToLower() == "json")
                    {
                        _serializerService = new JSONSerializerService();
                    }
                    if (DataFormat.ToLower() == "xml")
                    {
                        _serializerService = new XmlSerializerService();
                    }
                    _serializerService = new AutoDetectSerializerService();
                }
                return _serializerService;
            }
        }

        #region IComponentPresentationFactory members

        /// <summary>
        /// Create IComponentPresentation from a string representing that IComponentPresentation (XML or JSON)
        /// </summary>
        /// <param name="componentPresentationStringContent">content to deserialize into an IComponentPresentation</param>
        /// <returns></returns>
        public IComponentPresentation GetIComponentPresentationObject(string componentPresentationStringContent)
        {
            return SerializerService.Deserialize<ComponentPresentation>(componentPresentationStringContent);
        }

        public IComponentPresentation GetComponentPresentation(string componentUri, string templateUri = "")
        {
            LoggerService.Debug(">>GetComponentPresentation ({0})", LoggingCategory.Performance, componentUri);
            IComponentPresentation cp;
            if (!TryGetComponentPresentation(out cp, componentUri, templateUri))
            {
                LoggerService.Debug("<<GetComponentPresentation ({0}) -- not found", LoggingCategory.Performance, componentUri);
                throw new ComponentPresentationNotFoundException();
            }

            LoggerService.Debug("<<GetComponentPresentation ({0})", LoggingCategory.Performance, componentUri);
            return cp;
        }

        public IList<IComponentPresentation> FindComponentPresentations(IQuery queryParameters, int pageIndex, int pageSize, out int totalCount)
        {
            LoggerService.Debug(">>FindComponentPresentations ({0},{1})", LoggingCategory.Performance, queryParameters.ToString(), Convert.ToString(pageIndex));
            totalCount = 0;
            IList<string> results = ComponentPresentationProvider.FindComponents(queryParameters);
            totalCount = results.Count;

            var pagedResults = results
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .Select(c => { IComponentPresentation cp = null; TryGetComponentPresentation(out cp, c); return cp; })
                .Where(cp => cp != null)
                .ToList();

            LoggerService.Debug("<<FindComponentPresentations ({0},{1})", LoggingCategory.Performance, queryParameters.ToString(), Convert.ToString(pageIndex));
            return pagedResults;

        }


        public IList<IComponentPresentation> FindComponentPresentations(IQuery queryParameters)
        {
            LoggerService.Debug(">>FindComponentPresentations ({0})", LoggingCategory.Performance, queryParameters.ToString());

            var results = ComponentPresentationProvider.FindComponents(queryParameters)
                .Select(c => { IComponentPresentation cp = null; TryGetComponentPresentation(out cp, c); return cp; })
                .Where(cp => cp != null)
                .ToList();
            LoggerService.Debug("<<FindComponentPresentations ({0})", LoggingCategory.Performance, queryParameters.ToString());
            return results;
        }

        public DateTime GetLastPublishedDate(string componentUri, string templateUri = "")
        {
            return ComponentPresentationProvider.GetLastPublishedDate(componentUri);
        }

        [Obsolete]
        public override DateTime GetLastPublishedDateCallBack(string key, object cachedItem)
        {
            if (cachedItem == null)
                return DateTime.Now; // this will force the item to be removed from the cache
            if (cachedItem is IComponentPresentation)
            {
                return GetLastPublishedDate(((IComponentPresentation)cachedItem).Component.Id);
            }
            throw new Exception(string.Format("GetLastPublishedDateCallBack called for unexpected object type '{0}' or with unexpected key '{1}'", cachedItem.GetType(), key));
        }
        ///// <summary>
        ///// Get or set the CacheAgent
        ///// </summary>  
        //public override ICacheAgent CacheAgent
        //{
        //    get
        //    {
        //        if (_cacheAgent == null)
        //        {
        //            _cacheAgent = new NullCacheAgent();
        //            // the next line is the only reason we are overriding this property: to set a callback
        //            _cacheAgent.GetLastPublishDateCallBack = GetLastPublishedDateCallBack;
        //        }
        //        return _cacheAgent;
        //    }
        //    set
        //    {
        //        _cacheAgent = value;
        //        _cacheAgent.GetLastPublishDateCallBack = GetLastPublishedDateCallBack;
        //    }
        //}



        public bool TryGetComponentPresentation(out IComponentPresentation cp, string componentUri, string templateUri = "")
        {
            cp = null;

            string[] cacheUris = { componentUri };

            if (!String.IsNullOrEmpty(templateUri))
            {
                cacheUris = new string[] { componentUri, templateUri };
            }

            string cacheKey = CacheKeyFactory.GenerateKey(CacheRegion, cacheUris);

            cp = (IComponentPresentation)CacheAgent.Load(cacheKey);

            if (cp != null)
            {
                LoggerService.Debug("<<TryGetComponentPresentation ({0}) - from cache", LoggingCategory.Performance, componentUri);
                return true;
            }

            string content = !String.IsNullOrEmpty(templateUri) ?
                                        ComponentPresentationProvider.GetContent(componentUri, templateUri) :
                                        ComponentPresentationProvider.GetContent(componentUri);

            if (string.IsNullOrEmpty(content))
            {
                LoggerService.Debug("<<TryGetComponentPresentationOrComponent - no content found by provider for uris {0} and {1}", LoggingCategory.Performance, componentUri, templateUri);
                return false;
            }
            LoggerService.Debug("about to create IComponentPresentation from content ({0})", LoggingCategory.Performance, componentUri);
            cp = GetIComponentPresentationObject(content);
            LoggerService.Debug("finished creating IComponentPresentation from content ({0})", LoggingCategory.Performance, componentUri);

            // if there is no ComponentTemplate, the content of this CP probably represents a component instead of a component PRESENTATION
            // in that case, we should at least add the template uri method parameter (if there is one) to the object  
            if (cp.ComponentTemplate == null)
            {
                ((ComponentPresentation)cp).ComponentTemplate = new ComponentTemplate();
            }
            if (cp.ComponentTemplate.Id == null)
            {
                ((ComponentPresentation)cp).ComponentTemplate.Id = templateUri;
            }

            LoggerService.Debug("about to store IComponentPresentation in cache ({0})", LoggingCategory.Performance, componentUri);
            CacheAgent.Store(cacheKey, CacheRegion, cp, new List<string> { cp.Component.Id });
            LoggerService.Debug("finished storing IComponentPresentation in cache ({0})", LoggingCategory.Performance, componentUri);
            LoggerService.Debug("<<TryGetComponentPresentation ({0})", LoggingCategory.Performance, componentUri);

            return cp != null;
        }

        public IList<IComponentPresentation> GetComponentPresentations(string[] componentUris)
        {
            List<IComponentPresentation> cps = new List<IComponentPresentation>();
            foreach (string content in ComponentPresentationProvider.GetContentMultiple(componentUris))
            {
                cps.Add(GetIComponentPresentationObject(content));
            }
            return cps;
        }
        #endregion

    }
}
