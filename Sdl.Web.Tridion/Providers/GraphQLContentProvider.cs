using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Sdl.Tridion.Api.Client;
using Sdl.Tridion.Api.Client.ContentModel;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.DataModel;
using Sdl.Web.Tridion.PCAClient;
using Sdl.Web.Tridion.Providers.Query;
using Sdl.Web.Tridion.Statics;

namespace Sdl.Web.Tridion.Mapping
{
    /// <summary>
    /// GraphQL Content Provider implementation (based on Public Content Api).
    /// </summary>
    public class GraphQLContentProvider : IContentProvider, IRawDataProvider
    {
        #region Cursor Indexing
        internal class CursorIndexer
        {
            private const string SessionKey = "dxa_indexer";

            private readonly Dictionary<int, string> _cursors = new Dictionary<int, string>();

            public static CursorIndexer GetCursorIndexer(string id)
            {
                if (HttpContext.Current == null)
                    return new CursorIndexer(); // empty dummy
                var indexer = (Dictionary<string, CursorIndexer>)HttpContext.Current.Session[SessionKey] ?? new Dictionary<string, CursorIndexer>();
                if (!indexer.ContainsKey(id))
                {
                    indexer.Add(id, new CursorIndexer());
                }
                HttpContext.Current.Session[SessionKey] = indexer;
                return indexer[id];
            }

            public int StartIndex { get; private set; }

            public string this[int index]
            {
                get
                {
                    if (index == 0)
                    {
                        StartIndex = 0;
                        return null;
                    }
                    if (_cursors.Count == 0)
                    {
                        StartIndex = 0;
                        return null;
                    }
                    if (_cursors.ContainsKey(index))
                    {
                        StartIndex = index;
                        return _cursors[index];
                    }
                    int min = 0;
                    foreach (var x in _cursors.Keys)
                    {
                        if (x >= min && x < index) min = x;
                    }
                    StartIndex = min;
                    return StartIndex == 0 ? null : _cursors[StartIndex];
                }
                set
                {
                    if (_cursors.ContainsKey(index))
                    {
                        _cursors[index] = value;
                    }
                    else
                    {
                        _cursors.Add(index, value);
                    }
                }
            }
        }       
        #endregion

        private readonly IModelService _modelService;

        public GraphQLContentProvider()
        {
            _modelService = new Providers.ModelService.ModelService();
            ModelBuilderPipeline.Init();
        }

        /// <summary>
        /// Gets a Page Model for a given URL path.
        /// </summary>
        /// <param name="urlPath">The URL path (unescaped).</param>
        /// <param name="localization">The context <see cref="ILocalization"/>.</param>
        /// <param name="addIncludes">Indicates whether include Pages should be expanded.</param>
        /// <returns>The Page Model.</returns>
        /// <exception cref="DxaItemNotFoundException">If no Page Model exists for the given URL.</exception>
        public virtual PageModel GetPageModel(string urlPath, ILocalization localization, bool addIncludes = true)
            => _modelService.GetPageModel(urlPath, localization, addIncludes);

        /// <summary>
        /// Gets a Page Model for a given Page Id.
        /// </summary>
        /// <param name="pageId">Page Id</param>
        /// <param name="localization">The context Localization.</param>
        /// <param name="addIncludes">Indicates whether include Pages should be expanded.</param>
        /// <returns>The Page Model.</returns>
        /// <exception cref="DxaItemNotFoundException">If no Page Model exists for the given Id.</exception>
        public virtual PageModel GetPageModel(int pageId, ILocalization localization, bool addIncludes = true)
            => _modelService.GetPageModel(pageId, localization, addIncludes);

        /// <summary>
        /// Gets an Entity Model for a given Entity Identifier.
        /// </summary>
        /// <param name="id">The Entity Identifier. Must be in format {ComponentID}-{TemplateID}.</param>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The Entity Model.</returns>
        /// <exception cref="DxaItemNotFoundException">If no Entity Model exists for the given URL.</exception>
        public virtual EntityModel GetEntityModel(string id, ILocalization localization)
            => _modelService.GetEntityModel(id, localization);

        /// <summary>
        /// Gets a Static Content Item for a given URL path.
        /// </summary>
        /// <param name="urlPath">The URL path (unescaped).</param>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The Static Content Item.</returns>
        public virtual StaticContentItem GetStaticContentItem(string urlPath, ILocalization localization)
        {
            using (new Tracer(urlPath, localization))
            {
                string localFilePath = BinaryFileManager.Instance.GetCachedFile(urlPath, localization);

                return new StaticContentItem(
                    new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan),
                    MimeMapping.GetMimeMapping(localFilePath),
                    File.GetLastWriteTime(localFilePath),
                    Encoding.UTF8
                    );
            }
        }

        /// <summary>
        /// Gets a Static Content Item for a given Id.
        /// </summary>
        /// <param name="binaryId">The Id of the binary.</param>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The Static Content Item.</returns>
        public virtual StaticContentItem GetStaticContentItem(int binaryId, ILocalization localization)
        {
            using (new Tracer(binaryId, localization))
            {
                string localFilePath = BinaryFileManager.Instance.GetCachedFile(binaryId, localization);

                return new StaticContentItem(
                    new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan),
                    MimeMapping.GetMimeMapping(localFilePath),
                    File.GetLastWriteTime(localFilePath),
                    Encoding.UTF8
                    );
            }
        }

        /// <summary>
        /// Populates a Dynamic List by executing the query it specifies.
        /// </summary>
        /// <param name="dynamicList">The Dynamic List which specifies the query and is to be populated.</param>
        /// <param name="localization">The context Localization.</param>
        public virtual void PopulateDynamicList(DynamicList dynamicList, ILocalization localization)
        {
            using (new Tracer(dynamicList, localization))
            {              
                SimpleBrokerQuery simpleBrokerQuery = dynamicList.GetQuery(localization) as SimpleBrokerQuery;
                if (simpleBrokerQuery == null)
                {
                    throw new DxaException($"Unexpected result from {dynamicList.GetType().Name}.GetQuery: {dynamicList.GetQuery(localization)}");
                }

                // get our cursor indexer for this list
                var cursors = CursorIndexer.GetCursorIndexer(dynamicList.Id);

                // given our start index into the paged list we need to translate that to a cursor
                int start = simpleBrokerQuery.Start;
                simpleBrokerQuery.Cursor = cursors[start];

                // the cursor retrieved may of came from a different start index so we update start
                simpleBrokerQuery.Start = cursors.StartIndex;
                dynamicList.Start = cursors.StartIndex;
            
                var brokerQuery = new GraphQLQueryProvider();

                var components = brokerQuery.ExecuteQueryItems(simpleBrokerQuery).ToList();
                Log.Debug($"Broker Query returned {components.Count} results. HasMore={brokerQuery.HasMore}");

                if (components.Count > 0)
                {
                    Type resultType = dynamicList.ResultType;
                    dynamicList.QueryResults = components
                        .Select(
                            c =>
                                ModelBuilderPipeline.CreateEntityModel(
                                    CreateEntityModelData((Component) c), resultType,
                                    localization))
                        .ToList();
                }

                dynamicList.HasMore = brokerQuery.HasMore;

                if (brokerQuery.HasMore)
                {
                    // update cursor
                    cursors[simpleBrokerQuery.Start + simpleBrokerQuery.PageSize] = brokerQuery.Cursor;
                }
            }
        }

        protected virtual EntityModelData CreateEntityModelData(Component component)
        {
            ContentModelData standardMeta = new ContentModelData();
            foreach (var meta in component.CustomMetas.Edges)
            {
                standardMeta.Add(meta.Node.Key, meta.Node.Value);
            }

            // The semantic mapping requires that some metadata fields exist. This may not be the case so we map some component meta properties onto them
            // if they don't exist.
            if (!standardMeta.ContainsKey("dateCreated"))
            {
                standardMeta.Add("dateCreated", component.LastPublishDate);
            }
            const string dateTimeFormat = "MM/dd/yyyy HH:mm:ss";
            standardMeta["dateCreated"] = DateTime.ParseExact((string)standardMeta["dateCreated"], dateTimeFormat, null);
            if (!standardMeta.ContainsKey("name"))
            {
                standardMeta.Add("name", component.Title);
            }            
            return new EntityModelData
            {               
                Id = component.ItemId.ToString(),
                SchemaId = component.SchemaId.ToString(),
                Metadata = new ContentModelData { { "standardMeta", standardMeta } }
            };
        }

        string IRawDataProvider.GetPageContent(string urlPath, ILocalization localization)
        {
            using (new Tracer(urlPath, localization))
            {
                if (!urlPath.EndsWith(Constants.DefaultExtension) && !urlPath.EndsWith(".json"))
                {
                    urlPath += Constants.DefaultExtension;
                }

                var client = PCAClientFactory.Instance.CreateClient();
                // Important: The content we are getting back is not model based so we need to inform
                // the PCA so it doesn't attempt to treat it as a R2/DD4T model and attempt conversion
                // since this will fail and we'll end up with no content being returned.
                client.DefaultContentType = ContentType.RAW;
                try
                {
                    var page = client.GetPage(localization.Namespace(),
                        localization.PublicationId(), urlPath, null, ContentIncludeMode.IncludeDataAndRender, null);
                    return JsonConvert.SerializeObject(page.RawContent.Data);
                }
                catch (Exception)
                {
                    throw new DxaException($"Page URL path '{urlPath}' in Publication '{localization.Id}' returned no data.");
                }               
            }
        }
    }
}
