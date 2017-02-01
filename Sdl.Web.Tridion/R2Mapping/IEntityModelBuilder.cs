using System;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;
using Sdl.Web.DataModel;

namespace Sdl.Web.Tridion.R2Mapping
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
        /// <param name="entityModelData">The DXA R2 Data Model.</param>
        /// <param name="baseModelType">The base type for the Entity Model to build.</param>
        /// <param name="inputEntityModel">Strongly typed Entity Model created by preceding Entity Model Builder; is <c>null</c> for the first Entity Model Builder in the chain.</param>
        /// <param name="localization">The context <see cref="Localization"/>.</param>
        /// <returns>The strongly typed Entity Model. Will be of type <paramref name="baseModelType"/> or a subclass.</returns>
        EntityModel BuildEntityModel(EntityModelData entityModelData, Type baseModelType, EntityModel inputEntityModel, Localization localization);
    }
}
