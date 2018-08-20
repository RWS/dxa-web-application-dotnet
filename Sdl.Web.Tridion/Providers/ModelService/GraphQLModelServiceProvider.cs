using System;
using System.Web.Configuration;
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
        private const int DefaultDescendantDepth = 10;
        private readonly int _descendantDepth;

        public GraphQLModelServiceProvider()
        {
            _binder = new Binder();
            _descendantDepth = int.TryParse(
                WebConfigurationManager.AppSettings["sitemap-default-descendant-depth"], out _descendantDepth)
                ? _descendantDepth
                : DefaultDescendantDepth;
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
                    GetCanonicalUrlPath(urlPath, false),
                    ContentType.MODEL, DataModelType.R2, addIncludes ? PageInclusion.INCLUDE : PageInclusion.EXCLUDE,
                    false, null);
                return LoadModel<PageModelData>(json);
            }
            catch (PcaException)
            {
                // try index page
                var json = Client.GetPageModelData(GetNamespace(localization), int.Parse(localization.Id),
                  GetCanonicalUrlPath(urlPath, true),
                  ContentType.MODEL, DataModelType.R2, addIncludes ? PageInclusion.INCLUDE : PageInclusion.EXCLUDE,
                  false, null);
                return LoadModel<PageModelData>(json);
            }
        }

        public SitemapItem[] GetChildSitemapItems(string parentSitemapItemId, ILocalization localization, bool includeAncestors, int descendantLevels)
        {
            try
            {
                if (descendantLevels == 0)
                    return new SitemapItem[] { };
                var sitmapItems = Client.GetSitemapSubtree(GetNamespace(localization),
                    int.Parse(localization.Id), parentSitemapItemId, descendantLevels, true, null);
                if (sitmapItems?.Items != null)
                {
                    return Convert(sitmapItems).Items.ToArray();
                }
            }
            catch (PcaException)
            {

            }
            return new SitemapItem[] { };
        }

        public TaxonomyNode GetSitemapItem(ILocalization localization)
        {
            try
            {
                var client = Client;
                var ns = GetNamespace(localization);
                var publicationId = int.Parse(localization.Id);
                var root = client.GetSitemap(ns, publicationId, _descendantDepth, null);
                ExpandSitemap(client, ns, publicationId, root);
                var result = Convert(root);
                return result as TaxonomyNode;
            }
            catch (PcaException)
            {
                return null;
            }
        }

        protected void ExpandSitemap(IModelServicePluginApi client, ContentNamespace ns, int publicationId,
          TaxonomySitemapItem root)
        {
            if (root?.HasChildNodes == null || !root.HasChildNodes.Value) return;
            if (root.Items == null)
            {
                var subtree = client.GetSitemapSubtree(ContentNamespace.Sites, 8, root.Id, 10, false, null);
                root.Items = subtree.Items;
            }
            foreach (var x in root.Items)
            {
                ExpandSitemap(client, ns, publicationId, x as TaxonomySitemapItem);
            }
        }

        protected SitemapItem Convert(ISitemapItem item)
        {
            if (item == null) return null;

            SitemapItem result = null;

            if (item is TaxonomySitemapItem)
            {
                result = new TaxonomyNode();
            }
            else if (item is PageSitemapItem)
            {
                result = new SitemapItem();
            }

            result.Type = item.Type;
            result.Title = item.Title;
            result.Id = item.Id;
            result.OriginalTitle = item.OriginalTitle;
            result.Visible = item.Visible.Value;
            if (item.PublishedDate != null)
            {
                result.PublishedDate = DateTime.ParseExact(item.PublishedDate, "MM/dd/yyyy HH:mm:ss", null);
            }
            result.Url = item.Url;

            if (!(item is TaxonomySitemapItem)) return result;
            TaxonomySitemapItem tsi = (TaxonomySitemapItem)item;
            TaxonomyNode node = (TaxonomyNode)result;
            node.Key = tsi.Key;
            node.ClassifiedItemsCount = tsi.ClassifiedItemsCount ?? 0;
            node.Description = tsi.Description;
            node.HasChildNodes = tsi.HasChildNodes.HasValue && tsi.HasChildNodes.Value;
            node.IsAbstract = tsi.Abstract.HasValue && tsi.Abstract.Value;
            if (tsi.Items == null || tsi.Items.Count <= 0)
                return result;
            foreach (var x in tsi.Items)
            {
                result.Items.Add(Convert(x));
            }

            return result;
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

        private static string GetCanonicalUrlPath(string urlPath, bool tryIndexPage)
        {
            if (string.IsNullOrEmpty(urlPath) || urlPath.Equals("/"))
                return Constants.IndexPageUrlSuffix + Constants.DefaultExtension;

            if (urlPath.EndsWith("/"))
                return Constants.DefaultExtensionLessPageName + Constants.DefaultExtension;

            if (urlPath.EndsWith(Constants.DefaultExtension))
                return urlPath;

            if (urlPath.LastIndexOf(".", StringComparison.Ordinal) > 0)
                return urlPath;

            return tryIndexPage
                ? urlPath + "/" + Constants.DefaultExtensionLessPageName + Constants.DefaultExtension
                : urlPath + Constants.DefaultExtension;
        }
    }
}
