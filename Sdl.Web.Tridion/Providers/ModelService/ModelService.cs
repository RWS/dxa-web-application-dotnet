using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.DataModel;
using Sdl.Web.Tridion.Mapping;

namespace Sdl.Web.Tridion.Providers.ModelService
{
    /// <summary>
    /// Service to produce Page and Entity models using the configured ModelServiceProvider.
    /// </summary>
    public class ModelService : IModelService
    {
        /// <summary>
        /// Gets a Page Model for a given URL path.
        /// </summary>
        /// <param name="urlPath">The URL path (unescaped).</param>
        /// <param name="localization">The context <see cref="ILocalization"/>.</param>
        /// <param name="addIncludes">Indicates whether include Pages should be expanded.</param>
        /// <returns>The Page Model.</returns>
        /// <exception cref="DxaItemNotFoundException">If no Page Model exists for the given URL.</exception>
        public PageModel GetPageModel(string urlPath, ILocalization localization, bool addIncludes = true)
        {
            using (new Tracer(urlPath, localization, addIncludes))
            {
                PageModel result = null;
                if (CacheRegions.IsViewModelCachingEnabled)
                {
                    PageModel cachedPageModel = SiteConfiguration.CacheProvider.GetOrAdd(
                        $"{urlPath}:{addIncludes}", // Cache Page Models with and without includes separately
                        CacheRegions.PageModel,
                        () =>
                        {
                            PageModel pageModel = LoadPageModel(ref urlPath, addIncludes, localization);
                            if (pageModel.NoCache)
                            {
                                result = pageModel;
                                return null;
                            }
                            return pageModel;
                        }
                        );

                    if (cachedPageModel != null)
                    {
                        // Don't return the cached Page Model itself, because we don't want dynamic logic to modify the cached state.
                        result = (PageModel)cachedPageModel.DeepCopy();
                    }
                }
                else
                {
                    result = LoadPageModel(ref urlPath, addIncludes, localization);
                }

                if (SiteConfiguration.ConditionalEntityEvaluator != null)
                {
                    result.FilterConditionalEntities(localization);
                }

                return result;
            }
        }

        /// <summary>
        /// Gets a Page Model for a given Page Id.
        /// </summary>
        /// <param name="pageId">Page Id</param>
        /// <param name="localization">The context Localization.</param>
        /// <param name="addIncludes">Indicates whether include Pages should be expanded.</param>
        /// <returns>The Page Model.</returns>
        /// <exception cref="DxaItemNotFoundException">If no Page Model exists for the given Id.</exception>
        public PageModel GetPageModel(int pageId, ILocalization localization, bool addIncludes = true)
        {
            using (new Tracer(localization.Id, pageId, localization, addIncludes))
            {
                PageModel result = null;
                if (CacheRegions.IsViewModelCachingEnabled)
                {
                    PageModel cachedPageModel = SiteConfiguration.CacheProvider.GetOrAdd(
                        $"{localization.Id}-{pageId}:{addIncludes}", // Cache Page Models with and without includes separately
                        CacheRegions.PageModel,
                        () =>
                        {
                            PageModel pageModel = LoadPageModel(pageId, addIncludes, localization);
                            if (pageModel.NoCache)
                            {
                                result = pageModel;
                                return null;
                            }
                            return pageModel;
                        }
                        );

                    if (cachedPageModel != null)
                    {
                        // Don't return the cached Page Model itself, because we don't want dynamic logic to modify the cached state.
                        result = (PageModel)cachedPageModel.DeepCopy();
                    }
                }
                else
                {
                    result = LoadPageModel(pageId, addIncludes, localization);
                }

                if (SiteConfiguration.ConditionalEntityEvaluator != null)
                {
                    result.FilterConditionalEntities(localization);
                }

                return result;
            }
        }

        /// <summary>
        /// Gets an Entity Model for a given Entity Identifier.
        /// </summary>
        /// <param name="id">The Entity Identifier. Must be in format {ComponentID}-{TemplateID}.</param>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The Entity Model.</returns>
        /// <exception cref="DxaItemNotFoundException">If no Entity Model exists for the given URL.</exception>
        public EntityModel GetEntityModel(string id, ILocalization localization)
        {
            using (new Tracer(id, localization))
            {
                EntityModel result;
                if (CacheRegions.IsViewModelCachingEnabled)
                {
                    EntityModel cachedEntityModel = SiteConfiguration.CacheProvider.GetOrAdd(
                        $"{id}-{localization.Id}", // key
                        CacheRegions.EntityModel,
                        () => LoadEntityModel(id, localization)
                        );

                    // Don't return the cached Entity Model itself, because we don't want dynamic logic to modify the cached state.
                    result = (EntityModel)cachedEntityModel.DeepCopy();
                }
                else
                {
                    result = LoadEntityModel(id, localization);
                }

                return result;
            }
        }

        protected virtual PageModel LoadPageModel(ref string urlPath, bool addIncludes, ILocalization localization)
        {
            using (new Tracer(urlPath, addIncludes, localization))
            {
                PageModelData pageModelData = SiteConfiguration.ModelServiceProvider.GetPageModelData(urlPath, localization, addIncludes);

                if (pageModelData == null)
                {
                    throw new DxaItemNotFoundException(urlPath);
                }

                if (pageModelData.MvcData == null)
                {
                    throw new DxaException($"Data Model for Page '{pageModelData.Title}' ({pageModelData.Id}) contains no MVC data. Ensure that the Page is published using the DXA R2 TBBs.");
                }

                return ModelBuilderPipeline.CreatePageModel(pageModelData, addIncludes, localization);
            }
        }

        protected virtual PageModel LoadPageModel(int pageId, bool addIncludes, ILocalization localization)
        {
            using (new Tracer(pageId, addIncludes, localization))
            {
                PageModelData pageModelData = SiteConfiguration.ModelServiceProvider.GetPageModelData(pageId, localization, addIncludes);

                if (pageModelData == null)
                {
                    throw new DxaItemNotFoundException($"Page not found for publication id {localization.Id} and page id {pageId}");
                }

                if (pageModelData.MvcData == null)
                {
                    throw new DxaException($"Data Model for Page '{pageModelData.Title}' ({pageModelData.Id}) contains no MVC data. Ensure that the Page is published using the DXA R2 TBBs.");
                }

                return ModelBuilderPipeline.CreatePageModel(pageModelData, addIncludes, localization);
            }
        }

        protected virtual EntityModel LoadEntityModel(string id, ILocalization localization)
        {
            using (new Tracer(id, localization))
            {
                EntityModelData entityModelData = SiteConfiguration.ModelServiceProvider.GetEntityModelData(id, localization);

                if (entityModelData == null)
                {
                    throw new DxaItemNotFoundException(id);
                }

                EntityModel result = ModelBuilderPipeline.CreateEntityModel(entityModelData, null, localization);

                if (result.XpmMetadata != null)
                {
                    // Entity Models requested through this method are per definition "query based" in XPM terminology.
                    result.XpmMetadata["IsQueryBased"] = true; // TODO TSI-24: Do this in Model Service (or CM-side?)
                }

                return result;
            }
        }
    }
}
