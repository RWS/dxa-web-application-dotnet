using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Navigation;
using Sdl.Web.DataModel;
using Sdl.Web.PublicContentApi.ContentModel;
using Sdl.Web.PublicContentApi.Exceptions;
using Sdl.Web.PublicContentApi.ModelServicePlugin;
using Sdl.Web.PublicContentApi.Utils;
using Sdl.Web.Tridion.PCAClient;

namespace Sdl.Web.Tridion.ModelService
{
    public class GraphQLModelServiceProvider : IModelServiceProvider
    {
        private readonly Binder _binder;

        public GraphQLModelServiceProvider()
        {
             _binder = new Binder();
        }

        public void AddDataModelExtension(IDataModelExtension extension)
        {
            _binder.AddDataModelExtension(extension);
        }

        protected PublicContentApi.PublicContentApi Client => PCAClientFactory.Instance.CreateClient();

        protected ContentNamespace GetNamespace(ILocalization localization)
            => CmUri.NamespaceIdentiferToId(localization.CmUriScheme);

        public EntityModelData GetEntityModelData(string entityId, ILocalization localization)
        {
            try
            {
                var json = Client.GetEntityModelData(GetNamespace(localization), int.Parse(localization.Id),
                    int.Parse(entityId),
                    ContentType.MODEL, DataModelType.R2, DcpType.DEFAULT,
                    false, null);
                return LoadModel<EntityModelData>(json);
            }
            catch (PcaException)
            {
                return null;
            }
        }

        public PageModelData GetPageModelData(int pageId, ILocalization localization, bool addIncludes)
        {
            try
            {
                var json = Client.GetPageModelData(GetNamespace(localization), int.Parse(localization.Id), pageId,
                    ContentType.MODEL, DataModelType.R2, addIncludes ? PageInclusion.INCLUDE : PageInclusion.EXCLUDE,
                    false, null);
                return LoadModel<PageModelData>(json);
            }
            catch (PcaException)
            {
                return null;
            }
        }

        public PageModelData GetPageModelData(string urlPath, ILocalization localization, bool addIncludes)
        {
            try
            {
                var json = Client.GetPageModelData(GetNamespace(localization), int.Parse(localization.Id),
                    GetCanonicalUrlPath(urlPath),
                    ContentType.MODEL, DataModelType.R2, addIncludes ? PageInclusion.INCLUDE : PageInclusion.EXCLUDE,
                    false, null);
                return LoadModel<PageModelData>(json);
            }
            catch (PcaException)
            {
                return null;
            }
        }

        public SitemapItem[] GetChildSitemapItems(string parentSitemapItemId, ILocalization localization, bool includeAncestors, int descendantLevels)
        {
            try
            {
                var sitmapItems = Client.GetSitemapSubtree(GetNamespace(localization),
                    int.Parse(localization.Id), parentSitemapItemId, descendantLevels, null);
                if (sitmapItems != null && sitmapItems.Items != null)
                {
                    return Convert<List<ISitemapItem>, SitemapItem[]>(sitmapItems.Items);
                }
            }
            catch (PcaException)
            {
                
            }
            return new SitemapItem[] {};
        }

        public TaxonomyNode GetSitemapItem(ILocalization localization)
        {
            try
            {
                return Convert<TaxonomySitemapItem, TaxonomyNode>(
                    Client.GetSitemap(GetNamespace(localization), int.Parse(localization.Id), 10, null));
            }
            catch (PcaException)
            {
                return null;
            }
        }

        protected TOut Convert<TIn, TOut>(TIn item)
        {
            if (item == null)
                return default(TOut);

            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            return JsonConvert.DeserializeObject<TOut>(
                JsonConvert.SerializeObject(item, settings), settings);
        }

        protected T LoadModel<T>(dynamic json)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Binder = _binder,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            return JsonConvert.DeserializeObject<T>(json.ToString(), settings);
        }
    
        private static string GetCanonicalUrlPath(string urlPath)
        {
            if (string.IsNullOrEmpty(urlPath) || urlPath.Equals("/"))
                return Constants.IndexPageUrlSuffix + Constants.DefaultExtension;
          
            if (urlPath.EndsWith("/"))
                return Constants.DefaultExtensionLessPageName + Constants.DefaultExtension;

            if (urlPath.EndsWith(Constants.DefaultExtension))
                return urlPath;

            if (urlPath.LastIndexOf(".", StringComparison.Ordinal) > 0)
                return urlPath;

            return urlPath + Constants.IndexPageUrlSuffix + Constants.DefaultExtension;
        }
    }
}
