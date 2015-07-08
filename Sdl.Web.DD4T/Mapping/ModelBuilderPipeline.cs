using System;
using System.Collections.Generic;
using System.Configuration;
using DD4T.ContentModel;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.DD4T.Configuration;
using IPage = DD4T.ContentModel.IPage;

namespace Sdl.Web.DD4T.Mapping
{
    /// <summary>
    /// Represents a pipeline/chain of configured Strongly Typed View Model Builders (DD4T-based).
    /// </summary>
    /// <remarks>
    /// Each Model Builder in the pipeline is invoked and has the possibility to modify the resulting Page/Entity Model.
    /// The first Model Builder has to construct the View Models (it will get in <c>null</c>).
    /// Normally, the <see cref="DefaultModelBuilder"/> will be the first and only one.
    /// NOTE: The Model Builder pipeline is not a public extension point; it should only be used for advanced (SDL-owned) modules like the SmartTarget module.
    /// </remarks>
    /// <seealso cref="IModelBuilder"/>
    public static class ModelBuilderPipeline
    {
        private static readonly IEnumerable<IModelBuilder> _modelBuilders;

        /// <summary>
        /// Initializes the Model Builder Pipeline (class constructor).
        /// </summary>
        static ModelBuilderPipeline()
        {
            using (new Tracer())
            {
                IList<IModelBuilder> modelBuilders = new List<IModelBuilder>();
                try
                {
                    ModelBuilderPipelineConfiguration config = (ModelBuilderPipelineConfiguration)ConfigurationManager.GetSection(ModelBuilderPipelineConfiguration.SectionName);
                    if (config != null)
                    {
                        foreach (ModelBuilderSettings modelBuilderSettings in config.ModelBuilders)
                        {
                            Type modelBuilderType = Type.GetType(modelBuilderSettings.Type, throwOnError: true, ignoreCase: true);
                            IModelBuilder modelBuilder = (IModelBuilder)Activator.CreateInstance(modelBuilderType);
                            modelBuilders.Add(modelBuilder);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Throwing exceptions from a class constructor results in rather cryptic error messages, so we handle the exception here.
                    Log.Error(ex);
                    Log.Warn("An error occurred while initializing the Model Builder Pipeline. Using the Default Model Builder.");
                    modelBuilders.Clear();
                }

                if (modelBuilders.Count == 0)
                {
                    modelBuilders.Add(new DefaultModelBuilder());
                }

                _modelBuilders = modelBuilders;
            }
        }


        /// <summary>
        /// Creates a Strongly Typed Page Model for a given DD4T Page and an optional set of include Pages.
        /// </summary>
        /// <param name="page">The DD4T Page object.</param>
        /// <param name="includes">The set of DD4T Page object for include pages. Can be <c>null</c>.</param>
        /// <param name="localization">The context <see cref="Localization"/>.</param>
        /// <returns>The Strongly Typed Page Model (an instance of class <see cref="PageModel"/> or a subclass).</returns>
        public static PageModel CreatePageModel(IPage page, IPage[] includes, Localization localization)
        {
            using (new Tracer(page, includes, localization))
            {
                PageModel pageModel = null;
                foreach (IModelBuilder modelBuilder in _modelBuilders)
                {
                    modelBuilder.BuildPageModel(ref pageModel, page, includes, localization);
                }
                return pageModel;
            }
        }


        /// <summary>
        /// Creates a Strongly Typed Entity Model for a given DD4T Component Presentation.
        /// </summary>
        /// <param name="cp">The DD4T Component Presentation.</param>
        /// <param name="localization">The context <see cref="Localization"/>.</param>
        /// <returns>The Strongly Typed Entity Model.</returns>
        public static EntityModel CreateEntityModel(IComponentPresentation cp, Localization localization)
        {
            using (new Tracer(cp, localization))
            {
                EntityModel entityModel = null;
                foreach (IModelBuilder modelBuilder in _modelBuilders)
                {
                    modelBuilder.BuildEntityModel(ref entityModel, cp, localization);
                }
                return entityModel;
            }
        }


        /// <summary>
        /// Creates a Strongly Typed Entity Model for a given DD4T Component.
        /// </summary>
        /// <param name="component">The DD4T Component.</param>
        /// <param name="baseModelType">The (base) type for the Entity Model.</param>
        /// <param name="localization">The context <see cref="Localization"/>.</param>
        /// <returns>The Strongly Typed Entity Model (an instance of type <paramref name="baseModelType"/> or a subclass).</returns>
        public static EntityModel CreateEntityModel(IComponent component, Type baseModelType, Localization localization)
        {
            using (new Tracer(component, baseModelType, localization))
            {
                EntityModel entityModel = null;
                foreach (IModelBuilder modelBuilder in _modelBuilders)
                {
                    modelBuilder.BuildEntityModel(ref entityModel, component, baseModelType, localization);
                }
                return entityModel;
            }
        }
    }
}
