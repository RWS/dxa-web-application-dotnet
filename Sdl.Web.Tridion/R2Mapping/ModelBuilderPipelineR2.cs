using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Extensions;
using Sdl.Web.DataModel;
using Sdl.Web.Tridion.Configuration;
using Sdl.Web.Common.Utils;

namespace Sdl.Web.Tridion.R2Mapping
{
    /// <summary>
    /// Represents a pipeline/chain of configured Strongly Typed View Model Builders (based on DXA R2 Data Model).
    /// </summary>
    /// <remarks>
    /// Each Model Builder in the pipeline is invoked and has the possibility to modify the resulting Page/Entity Model.
    /// The first Model Builder has to construct the View Models (it will get in <c>null</c>).
    /// Normally, the <see cref="DefaultModelBuilderR2"/> will be the first and only one.
    /// </remarks>
    /// <seealso cref="IPageModelBuilder"/>
    /// <seealso cref="IEntityModelBuilder"/>
    public static class ModelBuilderPipelineR2
    {
        private static readonly IEnumerable<IPageModelBuilder> _pageModelBuilders;
        private static readonly IEnumerable<IEntityModelBuilder> _entityModelBuilders;

        /// <summary>
        /// Initializes the Model Builder Pipeline (class constructor).
        /// </summary>
        static ModelBuilderPipelineR2()
        {
            using (new Tracer())
            {
                IList<IPageModelBuilder> pageModelBuilders = new List<IPageModelBuilder>();
                IList<IEntityModelBuilder> entityModelBuilders = new List<IEntityModelBuilder>();
                try
                {
                    ModelBuilderPipelineConfiguration config = (ModelBuilderPipelineConfiguration)ConfigurationManager.GetSection(ModelBuilderPipelineConfiguration.SectionName);
                    if (config == null)
                    {
                        Log.Warn($"No '{ModelBuilderPipelineConfiguration.SectionName}' configuration section found.");
                    }
                    else
                    {
                        foreach (ModelBuilderSettings modelBuilderSettings in config.ModelBuilders)
                        {
                            Type modelBuilderType = Type.GetType(modelBuilderSettings.Type, throwOnError: true, ignoreCase: true);
                            object modelBuilder = modelBuilderType.CreateInstance();
                            IPageModelBuilder pageModelBuilder = modelBuilder as IPageModelBuilder;
                            IEntityModelBuilder entityModelBuilder = modelBuilder as IEntityModelBuilder;
                            if ((pageModelBuilder == null) && (entityModelBuilder == null))
                            {
                                Log.Warn($"Configured Model Builder Type '{modelBuilderType.FullName}' does not implement IPageModelBuilder nor IEntityModelBuilder; skipping.");
                                continue;
                            }
                            if (pageModelBuilder != null)
                            {
                                Log.Info($"Using Page Model Builder Type '{modelBuilderType.FullName}'");
                                pageModelBuilders.Add(pageModelBuilder);
                            }
                            if (entityModelBuilder != null)
                            {
                                Log.Info($"Using Entity Model Builder Type '{modelBuilderType.FullName}'");
                                entityModelBuilders.Add(entityModelBuilder);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Throwing exceptions from a class constructor results in rather cryptic error messages, so we handle the exception here.
                    Log.Error(ex);
                    Log.Warn("An error occurred while initializing the Model Builder Pipeline. Using the Default Model Builder only.");
                    pageModelBuilders.Clear();
                    entityModelBuilders.Clear();
                }

                // Ensure there is always at least one Page/Entity Model Buidler.
                if (!pageModelBuilders.Any() || !entityModelBuilders.Any())
                {
                    DefaultModelBuilderR2 defaultModelBuilder = new DefaultModelBuilderR2();
                    if (!pageModelBuilders.Any())
                    {
                        Log.Warn($"No Page Model Builders configured; using '{defaultModelBuilder.GetType().FullName}' only.");
                        pageModelBuilders.Add(defaultModelBuilder);
                    }
                    if (!entityModelBuilders.Any())
                    {
                        Log.Warn($"No Entity Model Builders configured; using '{defaultModelBuilder.GetType().FullName}' only.");
                        entityModelBuilders.Add(defaultModelBuilder);
                    }
                }

                _pageModelBuilders = pageModelBuilders;
                _entityModelBuilders = entityModelBuilders;
            }
        }


        /// <summary>
        /// Creates a Strongly Typed Page Model for a given DXA R2 Data Model.
        /// </summary>
        /// <param name="pageModelData">The DXA R2 Data Model.</param>
        /// <param name="includePageRegions">Indicates whether Include Page Regions should be included.</param>
        /// <param name="localization">The context <see cref="Localization"/>.</param>
        /// <returns>The Strongly Typed Page Model (an instance of class <see cref="PageModel"/> or a subclass).</returns>
        public static PageModel CreatePageModel(PageModelData pageModelData, bool includePageRegions,
            Localization localization)
        {
            using (new Tracer(pageModelData, localization))
            {
                PageModel pageModel = null;
                if (CacheRegions.IsViewModelCachingEnabled) // quick way to avoid all caching on viewmodels
                {
                    string key = $"{pageModelData.Id}-{includePageRegions}-{localization.Id}";
                    PageModel cachedPageModel = SiteConfiguration.CacheProvider.GetOrAdd(
                       key,
                       CacheRegions.PageModel,
                       () =>
                       {
                           pageModel = CreatePageModelInternal(pageModelData, includePageRegions, localization);
                           if (pageModel.NoCache || pageModel.IsVolatile || pageModel.HasNoCacheAttribute)
                           {
                               // this page has been marked to no caching so we return null to prevent a cache write                               
                               Log.Trace($"PageModel with id={pageModelData.Id} MvcData={pageModel.MvcData} includePageRegions={includePageRegions} localization={localization.Id} was marked for no caching.");
                               return null;
                           }
                           Log.Trace($"PageModel with id={pageModelData.Id} MvcData={pageModel.MvcData} includePageRegions={includePageRegions} localization={localization.Id} added to cache.");
                           return pageModel;
                       }
                       );

                    if (cachedPageModel != null)
                    {
                        // don't return the cached Page Model itself, because we don't want dynamic logic to modify the cached state.
                        pageModel = (PageModel)cachedPageModel.DeepCopy();
                    }
                }
                else
                {
                    pageModel = CreatePageModelInternal(pageModelData, includePageRegions, localization);
                }
                return pageModel;
            }
        }      
        
        private static int CalcHash(EntityModelData entityModelData)
        {
            int h0 = entityModelData.Id?.GetHashCode() ?? 0;
            int h1 = entityModelData.HtmlClasses?.GetHashCode() ?? 0;
            int h2 = (int)entityModelData.SerializationHashCode;
            int h3 = entityModelData.LinkUrl?.GetHashCode() ?? 0;
            int h4 = entityModelData.MvcData?.GetHashCode() ?? 0;

            return Hash.CombineHashCodes(h0, h1, h2, h3, h4);
        }

        /// <summary>
        /// Creates a Strongly Typed Entity Model for a given DXA R2 Data Model.
        /// </summary>
        /// <param name="entityModelData">The DXA R2 Data Model.</param>
        /// <param name="baseModelType">The base type for the Entity Model to build.</param>
        /// <param name="localization">The context <see cref="Localization"/>.</param>
        /// <returns>The strongly typed Entity Model. Will be of type <paramref name="baseModelType"/> or a subclass.</returns>
        public static EntityModel CreateEntityModel(EntityModelData entityModelData, Type baseModelType, Localization localization)
        {                        
            using (new Tracer(entityModelData, localization))
            {
                EntityModel entityModel = null;               
                if (CacheRegions.IsViewModelCachingEnabled) // quick way to avoid all caching on viewmodels
                {
                    string key = $"{localization.Id}-{CalcHash(entityModelData)}";
                    EntityModel cachedEntityModel = SiteConfiguration.CacheProvider.GetOrAdd(
                       key,
                       CacheRegions.EntityModel,
                       () =>
                       {
                           entityModel = CreateEntityModelInternal(entityModelData, baseModelType, localization);
                           if (entityModel.IsVolatile || entityModel.HasNoCacheAttribute)
                           {
                               // this entity has been marked for no caching so we return null to prevent a cache write                         
                               Log.Trace($"EntityModel with id={entityModelData.Id} MvcData={entityModel.MvcData} localization={localization.Id} was marked for no caching.");
                               entityModel.IsVolatile = true;
                               return null;
                           }
                           Log.Trace($"EntityModel with id={entityModelData.Id} MvcData={entityModel.MvcData} localization={localization.Id} was added to cache.");
                           return entityModel;
                       }
                       );

                    if (cachedEntityModel != null)
                    {
                        // don't return the cached Page Model itself, because we don't want dynamic logic to modify the cached state.
                        entityModel = (EntityModel)cachedEntityModel.DeepCopy();
                    }
                }
                else
                {
                    entityModel = CreateEntityModelInternal(entityModelData, baseModelType, localization);
                }
                return entityModel;
            }
        }

        internal static PageModel CreatePageModelInternal(PageModelData pageModelData, bool includePageRegions,
          Localization localization)
        {
            PageModel pageModel = null;
            foreach (IPageModelBuilder pageModelBuilder in _pageModelBuilders)
            {
                pageModelBuilder.BuildPageModel(ref pageModel, pageModelData, includePageRegions, localization);
            }
            if (pageModel == null)
            {
                throw new DxaException("Page Model is null after all Page Model Builders have been run.");
            }
            return pageModel;
        }

        internal static EntityModel CreateEntityModelInternal(EntityModelData entityModelData, Type baseModelType,
            Localization localization)
        {
            EntityModel entityModel = null;
            foreach (IEntityModelBuilder entityModelBuilder in _entityModelBuilders)
            {
                entityModelBuilder.BuildEntityModel(ref entityModel, entityModelData, baseModelType, localization);
            }
            if (entityModel == null)
            {
                throw new DxaException("Entity Model is null after all Entity Model Builders have been run.");
            }
            return entityModel;
        }
    }
}
