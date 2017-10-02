using System;
using System.Web.Configuration;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Navigation;
using Sdl.Web.DataModel;
using Sdl.Web.ModelService;
using Sdl.Web.ModelService.Request;

namespace Sdl.Web.Tridion.ModelService
{   
    public class DefaultModelServiceProvider : IModelServiceProvider
    {
        private const string ModelServiceName = "DXA Model Service";
        private const int DefaultRetryCount = 4;
        private const int DefaultTimeout = 10000;
        private readonly ModelServiceClient _modelServiceClient;

        #region Binder
        private class Binder : DataModelBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                switch (typeName)
                {
                    case "TaxonomyNodeModelData":
                        return typeof(TaxonomyNode);
                    case "SitemapItemModelData":
                        return typeof(SitemapItem);
                }
                return base.BindToType(assemblyName, typeName);
            }
        }
        #endregion

        public DefaultModelServiceProvider()
        {
            string uri = WebConfigurationManager.AppSettings["model-builder-service-uri"];
            int n;
            int retryCount = int.TryParse(
                WebConfigurationManager.AppSettings["model-builder-service-retries"], out n)
                ? n
                : DefaultRetryCount;

            int timeout = int.TryParse(
                WebConfigurationManager.AppSettings["model-builder-service-timeout"], out n)
                ? n
                : DefaultTimeout;
            _modelServiceClient = new ModelServiceClient(uri, retryCount, timeout);
            Log.Debug($"{ModelServiceName} found at URL '{_modelServiceClient.ModelServiceBaseUri}'");            
        }

        public PageModelData GetPageModelData(string urlPath, Localization localization, bool addIncludes)
        {
            try
            {
                PageModelRequest request = new PageModelRequest
                {
                    Path = urlPath,
                    CmUriScheme = localization.CmUriScheme,
                    PublicationId = int.Parse(localization.Id),
                    PageInclusion = addIncludes ? PageInclusion.INCLUDE : PageInclusion.EXCLUDE,
                    Binder = new Binder()
                };

                ModelServiceResponse<PageModelData> response = _modelServiceClient.PerformRequest<PageModelData>(request);
                if (response.Response != null) response.Response.SerializationHashCode = response.Hashcode;
                return response.Response;
            }
            catch (ModelServiceException e)
            {
                Log.Error("{0} returned an unexpected response for URL '{1}':\n{2} ", ModelServiceName, _modelServiceClient.ModelServiceBaseUri,
                   e.Message);
                throw new DxaException($"{ModelServiceName} returned an unexpected response.", e);
            }
            catch (ItemNotFoundException)
            {
                return null;
            }
        }

        public EntityModelData GetEntityModelData(string entityId, Localization localization)
        {
            try
            {
                EntityModelRequest request = new EntityModelRequest
                {
                    EntityId = entityId,
                    CmUriScheme = localization.CmUriScheme,
                    PublicationId = int.Parse(localization.Id),
                    Binder = new Binder()                    
                };

                ModelServiceResponse<EntityModelData> response = _modelServiceClient.PerformRequest<EntityModelData>(request);
                if (response.Response != null) response.Response.SerializationHashCode = response.Hashcode;
                return response.Response;
            }
            catch (ModelServiceException e)
            {
                Log.Error("{0} returned an unexpected response for URL '{1}':\n{2} ", ModelServiceName, _modelServiceClient.ModelServiceBaseUri,
                   e.Message);
                throw new DxaException($"{ModelServiceName} returned an unexpected response.", e);
            }
            catch (ItemNotFoundException)
            {
                return null;
            }
        }

        public TaxonomyNode GetSitemapItem(Localization localization)
        {
            try
            {
                SitemapItemModelRequest request = new SitemapItemModelRequest
                {
                    PublicationId = int.Parse(localization.Id),
                    Binder = new Binder()
                };
                return _modelServiceClient.PerformRequest<TaxonomyNode>(request).Response;
            }
            catch (ModelServiceException e)
            {
                Log.Error("{0} returned an unexpected response for URL '{1}':\n{2} ", ModelServiceName, _modelServiceClient.ModelServiceBaseUri,
                   e.Message);
                throw new DxaException($"{ModelServiceName} returned an unexpected response.", e);
            }
            catch (ItemNotFoundException)
            {
                return null;
            }
        }

        public SitemapItem[] GetChildSitemapItems(string parentSitemapItemId, Localization localization,
            bool includeAncestors, int descendantLevels)
        {
            try
            {
                SitemapChildItemModelRequest request = new SitemapChildItemModelRequest
                {
                    PublicationId = int.Parse(localization.Id),
                    ParentSitemapItemId = parentSitemapItemId,
                    IncludeAncestors = includeAncestors,
                    DescendantLevels = descendantLevels,
                    Binder = new Binder()
                };
                return _modelServiceClient.PerformRequest<SitemapItem[]>(request).Response;
            }
            catch (ModelServiceException e)
            {
                Log.Error("{0} returned an unexpected response for URL '{1}':\n{2} ", ModelServiceName, _modelServiceClient.ModelServiceBaseUri,
                   e.Message);
                throw new DxaException($"{ModelServiceName} returned an unexpected response.", e);
            }
            catch (ItemNotFoundException)
            {
                return null;
            }
        }
    }
}
