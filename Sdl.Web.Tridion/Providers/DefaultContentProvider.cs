using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.DataModel;
using Sdl.Web.Tridion.Providers.Query;
using Sdl.Web.Tridion.Statics;
using Tridion.ContentDelivery.DynamicContent;
using Tridion.ContentDelivery.DynamicContent.Query;
using Tridion.ContentDelivery.Meta;

namespace Sdl.Web.Tridion.Mapping
{
    /// <summary>
    /// Default Content Provider implementation (based on DXA R2 Data Model).
    /// </summary>
    public class DefaultContentProvider : IContentProvider, IRawDataProvider
    {
        private readonly IModelService _modelService;

        public DefaultContentProvider()
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

                Common.Interfaces.IQueryProvider brokerQuery = new BrokerQueryProvider();
                string[] componentUris = brokerQuery.ExecuteQuery(simpleBrokerQuery).ToArray();
                Log.Debug($"Broker Query returned {componentUris.Length} results. HasMore={brokerQuery.HasMore}");

                if (componentUris.Length > 0)
                {
                    Type resultType = dynamicList.ResultType;
                    ComponentMetaFactory componentMetaFactory = new ComponentMetaFactory(localization.GetCmUri());
                    dynamicList.QueryResults = componentUris
                        .Select(c => ModelBuilderPipeline.CreateEntityModel(CreateEntityModelData(componentMetaFactory.GetMeta(c)), resultType, localization))
                        .ToList();
                }

                dynamicList.HasMore = brokerQuery.HasMore;
            }
        }

        protected virtual EntityModelData CreateEntityModelData(IComponentMeta componentMeta)
        {
            ContentModelData standardMeta = new ContentModelData();
            foreach (DictionaryEntry entry in componentMeta.CustomMeta.NameValues)
            {
                standardMeta.Add(entry.Key.ToString(), ((NameValuePair) entry.Value).Value);
            }

            // The semantic mapping requires that some metadata fields exist. This may not be the case so we map some component meta properties onto them
            // if they don't exist.
            if (!standardMeta.ContainsKey("dateCreated"))
            {
                standardMeta.Add("dateCreated", componentMeta.LastPublicationDate);
            }
            if (!standardMeta.ContainsKey("name"))
            {
                standardMeta.Add("name", componentMeta.Title);
            }

            return new EntityModelData
            {
                Id = componentMeta.Id.ToString(),
                SchemaId = componentMeta.SchemaId.ToString(),
                Metadata = new ContentModelData { { "standardMeta", standardMeta } }
            };
        }

        public virtual string GetPageContent(string urlPath, ILocalization localization)
        {
            // TODO: let the DXA Model Service provide raw Page Content too (?)
            using (new Tracer(urlPath, localization))
            {
                if (!urlPath.EndsWith(Constants.DefaultExtension) && !urlPath.EndsWith(".json"))
                {
                    urlPath += Constants.DefaultExtension;
                }
                string escapedUrlPath = Uri.EscapeUriString(urlPath);
                global::Tridion.ContentDelivery.DynamicContent.Query.Query brokerQuery = new global::Tridion.ContentDelivery.DynamicContent.Query.Query
                {
                    Criteria = CriteriaFactory.And(new Criteria[]
                    {
                        new PageURLCriteria(escapedUrlPath),
                        new PublicationCriteria(Convert.ToInt32(localization.Id)),
                        new ItemTypeCriteria(64)
                    })
                };

                string[] pageUris = brokerQuery.ExecuteQuery();
                if (pageUris.Length == 0)
                {
                    return null;
                }
                if (pageUris.Length > 1)
                {
                    throw new DxaException($"Broker Query for Page URL path '{urlPath}' in Publication '{localization.Id}' returned {pageUris.Length} results.");
                }

                PageContentAssembler pageContentAssembler = new PageContentAssembler();
                return pageContentAssembler.GetContent(pageUris[0]);
            }
        }
    }
}
