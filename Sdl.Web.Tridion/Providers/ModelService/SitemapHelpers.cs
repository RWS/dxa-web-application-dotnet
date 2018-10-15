using System;
using System.Collections.Generic;
using System.Linq;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Navigation;
using Sdl.Web.PublicContentApi.ContentModel;

namespace Sdl.Web.Tridion.Providers.ModelService
{
    internal static class SitemapHelpers
    {
        internal static ISitemapItem GetEntireTree(PublicContentApi.PublicContentApi client, ContentNamespace ns, int pubId, int requestLevels)
        {
            ISitemapItem root = client.GetSitemap(ns, pubId, requestLevels, null);
            if (root == null) return null;
            List<ISitemapItem> leafNodes = GetLeafNodes(root);
            while (leafNodes.Count > 0)
            {
                TaxonomySitemapItem node = leafNodes[0] as TaxonomySitemapItem;
                leafNodes.RemoveAt(0);
                if (!node.HasChildNodes.HasValue || !node.HasChildNodes.Value) continue;
                var subtree = client.GetSitemapSubtree(ns, pubId, node.Id, requestLevels, Ancestor.NONE, null);
                if (node.Items == null) node.Items = new List<ISitemapItem>();
                node.Items.AddRange(subtree[0].Items ?? new List<ISitemapItem>());
                leafNodes.AddRange(GetLeafNodes(node));
            }

            return root;
        }

        internal static List<ISitemapItem> GetEntireTree(PublicContentApi.PublicContentApi client, ContentNamespace ns, int pubId, string parentSitemapId, bool includeAncestors, int requestLevels)
        {
            var rootsItems = client.GetSitemapSubtree(ns, pubId, parentSitemapId, requestLevels, includeAncestors ? Ancestor.INCLUDE : Ancestor.NONE, null);
            List<ISitemapItem> roots = rootsItems.Cast<ISitemapItem>().ToList();
            if (roots.Count == 0) return new List<ISitemapItem>();
            List<ISitemapItem> tempRoots = new List<ISitemapItem>(roots);
            int index = 0;
            while (index < tempRoots.Count)
            {
                ISitemapItem root = tempRoots[index];
                List<ISitemapItem> leafNodes = GetLeafNodes(root);
                foreach (var item in leafNodes)
                {
                    TaxonomySitemapItem n = item as TaxonomySitemapItem;
                    var children = client.GetSitemapSubtree(ns, pubId, n.Id, requestLevels, Ancestor.NONE, null);
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

        internal static List<ISitemapItem> GetLeafNodes(ISitemapItem rootNode)
        {
            List<ISitemapItem> leafNodes = new List<ISitemapItem>();
            if (!(rootNode is TaxonomySitemapItem)) return leafNodes;
            TaxonomySitemapItem root = (TaxonomySitemapItem)rootNode;
            if (root?.HasChildNodes == null || !root.HasChildNodes.Value) return leafNodes;
            if (root.HasChildNodes.Value && root.Items == null) return new List<ISitemapItem> { root };
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
            return leafNodes;
        }

        internal static void Prune(ISitemapItem root, int currentLevel, int descendantLevels)
        {
            var item = root as TaxonomySitemapItem;
            TaxonomySitemapItem tNode = item;
            if (tNode?.Items == null || tNode.Items.Count <= 0) return;
            if (currentLevel < descendantLevels)
            {
                foreach (var n in tNode.Items)
                {
                    Prune(n, currentLevel + 1, descendantLevels);
                }
            }
            else
            {
                tNode.Items.Clear();
            }
        }

        internal static ISitemapItem FindNode(List<ISitemapItem> root, string parentSitemapId)
        {
            foreach (var node in root)
            {
                if (node.Id == parentSitemapId) return node;
                var item = node as TaxonomySitemapItem;
                TaxonomySitemapItem tNode = item;
                if (tNode?.Items == null || tNode.Items.Count <= 0) continue;
                var found = FindNode(tNode.Items, parentSitemapId);
                if (found != null) return found;
            }
            return null;
        }

        #region Conversion       

        internal static SitemapItem[] Convert(List<TaxonomySitemapItem> items)
        {
            if (items == null) return new SitemapItem[] { };
            SitemapItem[] converted = new SitemapItem[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                converted[i] = Convert(items[i]);
            }
            return converted;
        }

        internal static SitemapItem[] Convert(List<ISitemapItem> items)
        {
            if (items == null) return new SitemapItem[] { };
            SitemapItem[] converted = new SitemapItem[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                converted[i] = Convert(items[i]);
            }
            return converted;
        }

        internal static SitemapItem Convert(ISitemapItem item)
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
            if (tsi.Items == null || tsi.Items.Count <= 0) return result;
            foreach (var x in tsi.Items)
            {
                result.Items.Add(Convert(x));
            }
            return result;
        }

        #endregion
    }
}
