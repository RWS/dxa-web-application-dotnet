using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Configuration;
using Newtonsoft.Json;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Navigation;
using Sdl.Web.DataModel;
using Sdl.Web.GraphQLClient.Exceptions;
using Sdl.Web.PublicContentApi;
using Sdl.Web.PublicContentApi.ContentModel;
using Sdl.Web.PublicContentApi.Exceptions;
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
                if (string.IsNullOrEmpty(entityId))
                    return null;

                // entityId is of form componentId-templateId
                string[] ids = entityId.Split('-');
                if (ids.Length != 2) return null;

                var json = Client.GetEntityModelData(GetNamespace(localization), int.Parse(localization.Id),
                    int.Parse(ids[0]), int.Parse(ids[1]),
                    ContentType.MODEL, DataModelType.R2, DcpType.DEFAULT,
                    true, null);
                return LoadModel<EntityModelData>(json);
            }
            catch (GraphQLClientException e)
            {
                const string msg = "PCA client returned an unexpected response when retrieving enity model data for id {0}.";
                Log.Error(msg, entityId, e.Message);
                throw new DxaException(string.Format(msg, entityId), e);
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
                    true, null);
                return LoadModel<PageModelData>(json);
            }
            catch (GraphQLClientException e)
            {
                const string msg =
                    "PCA client returned an unexpected response when retrieving page model data for page id {0}.";
                Log.Error(msg, pageId, e.Message);
                throw new DxaException(string.Format(msg, pageId), e);
            }
            catch (PcaException)
            {
                return null;
            }
        }

        public PageModelData GetPageModelData(string urlPath, ILocalization localization, bool addIncludes)
        {
            const string msg =
               "PCA client returned an unexpected response when retrieving page model data for page url {0}.";
            dynamic json;           
            try
            {
                // TODO: This could be fixed by sending two graphQL queries in a single go. Need to
                // wait for PCA client fix for this however
                json = Client.GetPageModelData(GetNamespace(localization), int.Parse(localization.Id),
                    GetCanonicalUrlPath(urlPath, true),
                    ContentType.MODEL, DataModelType.R2, addIncludes ? PageInclusion.INCLUDE : PageInclusion.EXCLUDE,
                    true, null);
            }
            catch (GraphQLClientException e)
            {
                Log.Error(msg, urlPath, e.Message);
                throw new DxaException(string.Format(msg, urlPath), e);
            }
            catch (PcaException)
            {
                try
                {
                    // try index page
                    json = Client.GetPageModelData(GetNamespace(localization), int.Parse(localization.Id),
                        GetCanonicalUrlPath(urlPath, false),
                        ContentType.MODEL, DataModelType.R2, addIncludes ? PageInclusion.INCLUDE : PageInclusion.EXCLUDE,
                        true, null);
                }
                catch (GraphQLClientException e)
                {
                    Log.Error(msg, urlPath, e.Message);
                    throw new DxaException(string.Format(msg, urlPath), e);
                }
                catch (PcaException)
                {
                    // no page found here, client will handle the details
                    return null;
                }
            }
            return LoadModel<PageModelData>(json);
        }

        public TaxonomyNode GetSitemapItem(ILocalization localization)
        {
            try
            {
                var client = Client;
                var ns = GetNamespace(localization);
                var publicationId = int.Parse(localization.Id);
                var root = client.GetSitemap(ns, publicationId, 1, null);
                if (root == null) return null;
                string parent = root.Id ?? root.Items[0].Id.Split('-')[0];
                var tree = GetChildSitemapItemsInternal(parent, localization, true, -1);
                return (TaxonomyNode)Convert(tree[0]);
            }
            catch (GraphQLClientException e)
            {
                const string msg = "PCA client returned an unexpected response when retrieving sitemap items for sitemap.";
                Log.Error(msg, e.Message);
                throw new DxaException(msg, e);
            }
            catch (PcaException)
            {
                return null;
            }
        }

        public SitemapItem[] GetChildSitemapItems(string parentSitemapItemId, ILocalization localization, bool includeAncestors, int descendantLevels) 
            => Convert(GetChildSitemapItemsInternal(parentSitemapItemId, localization, includeAncestors,
            descendantLevels));

        protected List<ISitemapItem> GetChildSitemapItemsInternal(string parentSitemapItemId, ILocalization localization, bool includeAncestors, int descendantLevels)
        {
            try
            {
                int pubId = int.Parse(localization.Id);
                ContentNamespace ns = GetNamespace(localization);

                if (descendantLevels == -1)
                {
                    var tree = GetEntireTree(ns, pubId, parentSitemapItemId, includeAncestors);
                    if (parentSitemapItemId == null) return tree;
                    if (parentSitemapItemId.Split('-').Length == 1) return tree;
                    List<ISitemapItem> items = new List<ISitemapItem>();
                    foreach (TaxonomySitemapItem x in tree.OfType<TaxonomySitemapItem>())
                    {
                        items.AddRange(x.Items);
                    }
                    return items;
                }
                else
                {
                    if (parentSitemapItemId == null && descendantLevels > 0)
                        descendantLevels--;

                    if (parentSitemapItemId == null)
                    {
                        // requesting from root so just return descendants from root
                        var tree = Client.GetSitemapSubtree(ns, pubId, parentSitemapItemId, descendantLevels,
                            includeAncestors, null);
                        return tree.Cast<ISitemapItem>().ToList();
                    }

                    if (includeAncestors)
                    {
                        // we are looking for a particular item, we need to request the entire
                        // subtree first
                        var subtree = GetEntireTree(ns, pubId, parentSitemapItemId, true);

                        // now we prune descendants from our deseried node
                        ISitemapItem node = FindNode(subtree, parentSitemapItemId);
                        Prune(node, 0, descendantLevels);

                        return subtree;
                    }
                    else
                    {
                        var tree = Client.GetSitemapSubtree(ns, pubId, parentSitemapItemId, descendantLevels, false, null);

                        List<ISitemapItem> items = new List<ISitemapItem>();
                        foreach (TaxonomySitemapItem x in tree.Where(x => x.Items != null))
                        {
                            items.AddRange(x.Items);
                        }
                        return items;
                    }
                }
            }
            catch (GraphQLClientException e)
            {
                const string msg =
                    "PCA client returned an unexpected response when retrieving child sitemap items for sitemap id {0}.";
                Log.Error(msg, parentSitemapItemId, e.Message);
                throw new DxaException(string.Format(msg, parentSitemapItemId), e);
            }
            catch (PcaException)
            {

            }
            return new List<ISitemapItem>();
        }

        #region Helpers
        protected virtual void Prune(ISitemapItem root, int currentLevel, int descendantLevels)
        {
            var item = root as TaxonomySitemapItem;
            TaxonomySitemapItem tNode = item;
            if (tNode?.Items == null || tNode.Items.Count <= 0) return;

            if (currentLevel < descendantLevels)
            {                               
                foreach (var n in tNode.Items)
                {
                    Prune(n, currentLevel+1, descendantLevels);
                }
            }
            else
            {
                tNode.Items.Clear();
            }
        }

        protected virtual ISitemapItem FindNode(List<ISitemapItem> root, string parentSitemapId)
        {
            foreach (var node in root)
            {
                if (node.Id == parentSitemapId)
                    return node;

                var item = node as TaxonomySitemapItem;
                TaxonomySitemapItem tNode = item;
                if (tNode?.Items == null || tNode.Items.Count <= 0) continue;
                var found = FindNode(tNode.Items, parentSitemapId);
                if (found != null)
                    return found;
            }
            return null;
        }

        protected virtual List<ISitemapItem> GetEntireTree(ContentNamespace ns, int pubId, string parentSitemapId, bool includeAncestors)
        {
            var rootsItems = Client.GetSitemapSubtree(ns, pubId, parentSitemapId, _descendantDepth, includeAncestors, null);
            List<ISitemapItem> roots = rootsItems.Cast<ISitemapItem>().ToList();

            if (roots.Count == 0)
                return new List<ISitemapItem>();

            List<ISitemapItem> tempRoots = new List<ISitemapItem>(roots);
            int index = 0;
            while (index < tempRoots.Count)
            {
                ISitemapItem root = tempRoots[index];

                List<ISitemapItem> leafNodes = GetLeafNodes(root);
                foreach (var item in leafNodes)
                {
                    TaxonomySitemapItem n = item as TaxonomySitemapItem;
                    var children = Client.GetSitemapSubtree(ns, pubId, n.Id, _descendantDepth,
                        false, null);
                    if (children == null) continue;
                    n.Items = children[0].Items;
                    List<ISitemapItem> leaves = GetLeafNodes(n);
                    if (leaves == null || leaves.Count <= 0) continue;
                    tempRoots.AddRange(leaves.OfType<TaxonomySitemapItem>().Select(x => x));
                }
                index++;
            }
            return roots;
        }

        protected virtual List<ISitemapItem> GetLeafNodes(ISitemapItem rootNode)
        {
            List<ISitemapItem> leafNodes = new List<ISitemapItem>();

            if (rootNode is TaxonomySitemapItem)
            {
                TaxonomySitemapItem root = rootNode as TaxonomySitemapItem;
                if (root?.HasChildNodes == null || !root.HasChildNodes.Value)
                    return new List<ISitemapItem> {};

                if (root.HasChildNodes.Value && root.Items == null)
                    return new List<ISitemapItem> {root};

                foreach (var item in root.Items)
                {
                    TaxonomySitemapItem node = null;
                    if (item is TaxonomySitemapItem)
                    {
                        node = item as TaxonomySitemapItem;
                    }

                    if (node?.HasChildNodes == null || !node.HasChildNodes.Value) continue;
                    if (node.Items == null)
                        leafNodes.Add(node);
                    else
                    {
                        leafNodes.AddRange(GetLeafNodes(node));
                    }
                }
            }

            return leafNodes;
        }
        #endregion

        #region Conversion
        protected SitemapItem[] Convert(List<TaxonomySitemapItem> items)
        {
            if (items == null)
                return new SitemapItem[] { };
            SitemapItem[] converted = new SitemapItem[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                converted[i] = Convert(items[i]);
            }
            return converted;
        }

        protected SitemapItem[] Convert(List<ISitemapItem> items)
        {
            if(items == null)
                return new SitemapItem[] {};
            SitemapItem[] converted = new SitemapItem[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                converted[i] = Convert(items[i]);
            }
            return converted;
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
        #endregion

        protected T LoadModel<T>(dynamic json)
        {
            if (json == null) return default(T);
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
            if (urlPath == null)
                return "/" + Constants.DefaultExtensionLessPageName + Constants.DefaultExtension;

            if (urlPath.EndsWith(Constants.DefaultExtension))
                return urlPath;

            if (urlPath.LastIndexOf(".", StringComparison.Ordinal) > 0)
                return urlPath;

            if (!urlPath.StartsWith("/"))
                urlPath = "/" + urlPath;

            urlPath = urlPath.TrimEnd('/');

            if (string.IsNullOrEmpty(urlPath))
                return "/" + Constants.DefaultExtensionLessPageName + Constants.DefaultExtension;

            return tryIndexPage
               ? urlPath + "/" + Constants.DefaultExtensionLessPageName + Constants.DefaultExtension
               : urlPath + Constants.DefaultExtension;         
        }
    }
}
