using System;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;
using Sdl.Web.DataModel;

namespace Sdl.Web.Tridion.Mapping
{
    /// <summary>
    /// Interface for Entity Model Builders based on the DXA R2 Data Model.
    /// </summary>
    /// <seealso cref="ModelBuilderPipeline"/>
    public interface IEntityModelBuilder
    {
        /// <summary>
        /// Builds a strongly typed Entity Model based on a given DXA R2 Data Model.
        /// </summary>
        /// <param name="entityModel">The strongly typed Entity Model to build. Is <c>null</c> for the first Entity Model Builder in the pipeline.</param>
        /// <param name="entityModelData">The DXA R2 Data Model.</param>
        /// <param name="baseModelType">The base type for the Entity Model to build.</param>
        /// <param name="localization">The context <see cref="ILocalization"/>.</param>
        void BuildEntityModel(ref EntityModel entityModel, EntityModelData entityModelData, Type baseModelType, Localization localization);
    }
}
