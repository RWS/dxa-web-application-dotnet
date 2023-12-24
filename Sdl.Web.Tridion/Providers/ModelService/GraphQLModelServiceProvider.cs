using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Configuration;
using Newtonsoft.Json;
using Sdl.Tridion.Api.Client;
using Sdl.Tridion.Api.Client.ContentModel;
using Sdl.Tridion.Api.Client.Exceptions;
using Sdl.Tridion.Api.GraphQL.Client.Exceptions;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Navigation;
using Sdl.Web.DataModel;
using Sdl.Web.Tridion.ApiClient;
using Sdl.Web.Tridion.Providers.ModelService;

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

        protected Sdl.Tridion.Api.Client.ApiClient Client
        {
            get
            {
                var client = ApiClientFactory.Instance.CreateClient();
                client.DefaultModelType = DataModelType.R2;
                client.DefaultContentType = ContentType.MODEL;
                client.ModelSericeLinkRenderingType = ModelServiceLinkRendering.Relative;
                client.TcdlLinkRenderingType = TcdlLinkRendering.Relative;
                return client;
            }
        }

        public EntityModelData GetEntityModelData(string entityId, Localization localization)
        {
            try
            {
                if (string.IsNullOrEmpty(entityId))
                    return null;

                // entityId is of form componentId-templateId
                string[] ids = entityId.Split('-');
                if (ids.Length != 2) return null;

                var json = Client.GetEntityModelData(localization.Namespace(), localization.PublicationId(),
                    int.Parse(ids[0]), int.Parse(ids[1]),                    
                    ContentIncludeMode.IncludeDataAndRender, null);
                return LoadModel<EntityModelData>(json);
            }
            catch (GraphQLClientException e)
            {
                const string msg = "PCA client returned an unexpected response when retrieving enity model data for id {0}.";
                Log.Error(msg, entityId, e.Message);
                throw new DxaException(string.Format(msg, entityId), e);
            }
            catch (ApiException)
            {
                return null;
            }
        }

        public PageModelData GetPageModelData(int pageId, Localization localization, bool addIncludes)
        {
            try
            {
                var json = Client.GetPageModelData(localization.Namespace(), localization.PublicationId(), pageId,
                    addIncludes ? PageInclusion.INCLUDE : PageInclusion.EXCLUDE,
                    ContentIncludeMode.IncludeDataAndRender, null);
                return LoadModel<PageModelData>(json);
            }
            catch (GraphQLClientException e)
            {
                const string msg =
                    "PCA client returned an unexpected response when retrieving page model data for page id {0}.";
                Log.Error(msg, pageId, e.Message);
                throw new DxaException(string.Format(msg, pageId), e);
            }
            catch (ApiException)
            {
                return null;
            }
        }

        public PageModelData GetPageModelData(string urlPath, Localization localization, bool addIncludes)
        {
            const string msg =
               "PCA client returned an unexpected response when retrieving page model data for page url {0} or {1}.";
            dynamic json;

            // DXA supports "extensionless URLs" and "index pages".
            // For example: the index Page at root level (aka the Home Page) has a CM URL path of /index.html
            // It can be addressed in the DXA web application in several ways:
            //      1. /index.html – exactly the same as the CM URL
            //      2. /index – the file extension doesn't have to be specified explicitly {"extensionless URLs")
            //      3. / - the file name of an index page doesn't have to be specified explicitly either.
            // Note that the third option is the most clean one and considered the "canonical URL" in DXA; links to an index Page will be generated like that.
            // The problem with these "URL compression" features is that if a URL does not end with a slash (nor an extension), you don't 
            // know upfront if the URL addresses a regular Page or an index Page (within a nested SG).  
            // To determine this, DXA first tries the regular Page and if it doesn't exist, it appends /index.html and tries again.
            // TODO: The above should be handled by PCA (See CRQ-11703)
            try
            {
                json = Client.GetPageModelData(localization.Namespace(), localization.PublicationId(),
                    GetCanonicalUrlPath(urlPath, true),
                    addIncludes ? PageInclusion.INCLUDE : PageInclusion.EXCLUDE,
                    ContentIncludeMode.IncludeDataAndRender, null);
            }
            catch (Exception)
            {
                try
                {
                    json = Client.GetPageModelData(localization.Namespace(), localization.PublicationId(),
                        GetCanonicalUrlPath(urlPath, false),
                        addIncludes ? PageInclusion.INCLUDE : PageInclusion.EXCLUDE,
                        ContentIncludeMode.IncludeDataAndRender, null);
                }
                catch (GraphQLClientException e)
                {
                    Log.Error(msg, urlPath, e.Message);
                    throw new DxaException(
                        string.Format(msg, GetCanonicalUrlPath(urlPath, true), 
                        GetCanonicalUrlPath(urlPath, false)), e);
                }
                catch (ApiException)
                {
                    // no page found here, client will handle the details
                    return null;
                }
            }
            return LoadModel<PageModelData>(json);
        }

        public TaxonomyNode GetSitemapItem(Localization localization)
        {
            try
            {
                var ns = localization.Namespace();
                var publicationId = localization.PublicationId();
                var tree = SitemapHelpers.GetEntireTree(Client, ns, publicationId, _descendantDepth);
                return (TaxonomyNode)SitemapHelpers.Convert(tree);
            }
            catch (GraphQLClientException e)
            {
                const string msg = "PCA client returned an unexpected response when retrieving sitemap items for sitemap.";
                Log.Error(msg, e.Message);
                throw new DxaException(msg, e);
            }
            catch (ApiException)
            {
                return null;
            }
        }

        public SitemapItem[] GetChildSitemapItems(string parentSitemapItemId, Localization localization, bool includeAncestors, int descendantLevels)
            => SitemapHelpers.Convert(
                    GetChildSitemapItemsInternal(
                        parentSitemapItemId, localization, includeAncestors, descendantLevels
                    )
               );

        /// <summary>
        /// Replicate the behavior of the CIL implementation when it comes to requesting items rooted at
        /// a point with a specific depth level + include ancestors
        /// </summary>
        protected List<ISitemapItem> GetChildSitemapItemsInternal(string parentSitemapItemId, Localization localization, bool includeAncestors, int descendantLevels)
        {
            try
            {
                int pubId = localization.PublicationId();
                ContentNamespace ns = localization.Namespace();

                // Check if we are requesting the entire tree
                if (descendantLevels == -1)
                {
                    var tree0 = SitemapHelpers.GetEntireTree(Client, ns, pubId, parentSitemapItemId, includeAncestors, _descendantDepth);
                    // If parent node not specified we return entire tree
                    if (parentSitemapItemId == null) return tree0;        
                    // root node specified so return the direct children of this node
                    List<ISitemapItem> items0 = new List<ISitemapItem>();
                    foreach (TaxonomySitemapItem x in tree0.OfType<TaxonomySitemapItem>())
                    {
                        items0.AddRange(x.Items);
                    }
                    items0 = items0.OrderBy(i => i.OriginalTitle).ToList();
                    return items0;
                }

                if (parentSitemapItemId == null && descendantLevels > 0)
                    descendantLevels--;

                if (parentSitemapItemId == null)
                {
                    // Requesting from root so just return descendants from root
                    var tree0 = Client.GetSitemapSubtree(ns, pubId, null, descendantLevels, includeAncestors ? Ancestor.INCLUDE : Ancestor.NONE, null);
                    return tree0.Cast<ISitemapItem>().ToList();
                }

                if (includeAncestors)
                {
                    // We are looking for a particular item, we need to request the entire
                    // subtree first
                    var subtree0 = SitemapHelpers.GetEntireTree(Client, ns, pubId, parentSitemapItemId, true, _descendantDepth);

                    // Prune descendants from our deseried node
                    ISitemapItem node = SitemapHelpers.FindNode(subtree0, parentSitemapItemId);
                    SitemapHelpers.Prune(node, 0, descendantLevels);
                    return subtree0;
                }

                var tree = Client.GetSitemapSubtree(ns, pubId, parentSitemapItemId, descendantLevels, Ancestor.NONE, null);
                List<ISitemapItem> items = new List<ISitemapItem>();
                foreach (TaxonomySitemapItem x in tree.Where(x => x.Items != null))
                {
                    items.AddRange(x.Items);
                }
                items = items.OrderBy(i => i.OriginalTitle).ToList();
                return items;
            }
            catch (GraphQLClientException e)
            {
                const string msg =
                    "PCA client returned an unexpected response when retrieving child sitemap items for sitemap id {0}.";
                Log.Error(msg, parentSitemapItemId, e.Message);
                throw new DxaException(string.Format(msg, parentSitemapItemId), e);
            }
            catch (ApiException)
            {

            }
            return new List<ISitemapItem>();
        }

        protected T LoadModel<T>(dynamic json)
        {
            if (json == null) return default(T);
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto,
                SerializationBinder = (Newtonsoft.Json.Serialization.ISerializationBinder)_binder, // Use SerializationBinder instead of Binder
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