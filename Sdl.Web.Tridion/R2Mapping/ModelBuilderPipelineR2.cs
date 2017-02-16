using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Extensions;
using Sdl.Web.DataModel;
using Sdl.Web.Tridion.Configuration;

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
                                Log.Error($"Configured Model Builder Type '{modelBuilderType.FullName}' does not implement IPageModelBuilder nor IEntityModelBuilder; skipping.");
                                continue;
                            }
                            if (pageModelBuilder != null)
                            {
                                pageModelBuilders.Add(pageModelBuilder);
                            }
                            if (entityModelBuilder != null)
                            {
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
                        pageModelBuilders.Add(defaultModelBuilder);
                    }
                    if (!entityModelBuilders.Any())
                    {
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
        public static PageModel CreatePageModel(PageModelData pageModelData, bool includePageRegions, Localization localization)
        {
            using (new Tracer(pageModelData, localization))
            {
                PageModel pageModel = null;
                foreach (IPageModelBuilder pageModelBuilder in _pageModelBuilders)
                {
                    pageModelBuilder.BuildPageModel(ref pageModel, pageModelData, includePageRegions, localization);
                }
                return pageModel;
            }
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
                foreach (IEntityModelBuilder entityModelBuilder in _entityModelBuilders)
                {
                    entityModelBuilder.BuildEntityModel(ref entityModel, entityModelData, baseModelType, localization);
                }
                return entityModel;
            }
        }
    }
}
