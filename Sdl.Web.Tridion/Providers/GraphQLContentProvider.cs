using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Sdl.Tridion.Api.Client;
using Sdl.Tridion.Api.Client.ContentModel;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.DataModel;
using Sdl.Web.Tridion.ApiClient;
using Sdl.Web.Tridion.Providers.Query;

namespace Sdl.Web.Tridion.Mapping
{
    /// <summary>
    /// GraphQL Content Provider implementation (based on Public Content Api).
    /// </summary>
    public class GraphQLContentProvider : DefaultContentProvider
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
      
        /// <summary>
        /// Populates a Dynamic List by executing the query it specifies.
        /// </summary>
        /// <param name="dynamicList">The Dynamic List which specifies the query and is to be populated.</param>
        /// <param name="localization">The context Localization.</param>
        public override void PopulateDynamicList(DynamicList dynamicList, ILocalization localization)
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

        public override string GetPageContent(string urlPath, ILocalization localization)
        {
            using (new Tracer(urlPath, localization))
            {
                if (!urlPath.EndsWith(Constants.DefaultExtension) && !urlPath.EndsWith(".json"))
                {
                    urlPath += Constants.DefaultExtension;
                }

                var client = ApiClientFactory.Instance.CreateClient();
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
