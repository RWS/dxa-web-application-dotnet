using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Navigation;
using Sdl.Web.DataModel;
using Sdl.Web.PublicContentApi.ContentModel;
using Sdl.Web.PublicContentApi.ModelServicePlugin;
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

        public EntityModelData GetEntityModelData(string entityId, ILocalization localization)
        {          
            var json = Client.GetEntityModelData(ContentNamespace.Sites, int.Parse(localization.Id), int.Parse(entityId),
                ContentType.MODEL, DataModelType.R2, DcpType.DEFAULT,
                false, null);
            return LoadModel<EntityModelData>(json);
        }

        public PageModelData GetPageModelData(int pageId, ILocalization localization, bool addIncludes)
        {           
            var json = Client.GetPageModelData(ContentNamespace.Sites, int.Parse(localization.Id), pageId,
                ContentType.MODEL, DataModelType.R2, addIncludes ? PageInclusion.INCLUDE : PageInclusion.EXCLUDE,
                false, null);
            return LoadModel<PageModelData>(json);
        }

        public PageModelData GetPageModelData(string urlPath, ILocalization localization, bool addIncludes)
        {          
            var json = Client.GetPageModelData(ContentNamespace.Sites, int.Parse(localization.Id), 
                GetCanonicalUrlPath(urlPath), 
                ContentType.MODEL, DataModelType.R2, addIncludes ? PageInclusion.INCLUDE : PageInclusion.EXCLUDE,
                false, null);
            return LoadModel<PageModelData>(json);
        }

        public SitemapItem[] GetChildSitemapItems(string parentSitemapItemId, ILocalization localization, bool includeAncestors, int descendantLevels)
        {
           return new SitemapItem[] {};
        }

        public TaxonomyNode GetSitemapItem(ILocalization localization)
        {
            var taxonomy = Client.GetSitemap(ContentNamespace.Sites, int.Parse(localization.Id), 10, null);
            TaxonomyNode node = Convert(null, taxonomy);
            return node;
        }

        public TaxonomyNode Convert(TaxonomyNode parent, ISitemapItem item)
        {
            TaxonomyNode node = new TaxonomyNode
            {
                Type = item.Type,
                Id = item.Id,               
                OriginalTitle = item.OriginalTitle,
                Parent = parent,
                Title = item.Title,
                PublishedDate = DateTime.Parse(item.PublishedDate),
                Url = item.Url,
                Visible = item.Visible.Value,                
            };


            if (!(item is TaxonomySitemapItem)) return node;
            TaxonomySitemapItem sitemapItem = (TaxonomySitemapItem)item;
                
            node.ClassifiedItemsCount = sitemapItem.ClassifiedItemsCount.Value;
            node.Description = sitemapItem.Description;
            node.HasChildNodes = sitemapItem.HasChildNodes.Value;
            node.IsAbstract = sitemapItem.Abstract.Value;
            node.Key = sitemapItem.Key;
            if (!node.HasChildNodes) return node;
            node.Items = new List<SitemapItem>();
            foreach (var child in sitemapItem.Items)
            {
                node.Items.Add(Convert(node, child));
            }
            return node;
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

        private const string DefaultExtensionLessPageName = "index";
        private const string DefaultExtension = ".html";
        private const string IndexPageUrlSuffix = "/" + DefaultExtensionLessPageName;
        private static string GetCanonicalUrlPath(string urlPath)
        {
            string result = urlPath ?? IndexPageUrlSuffix;

            result = result.TrimStart('/');

            if (string.IsNullOrEmpty(result))
                return IndexPageUrlSuffix + DefaultExtension;

            if (result.EndsWith("/"))
            {
                result += DefaultExtensionLessPageName;
            }
            else if (result.EndsWith(DefaultExtension))
            {
                result = result.Substring(0, result.Length - DefaultExtension.Length);
            }
            return result;
        }
    }
}
