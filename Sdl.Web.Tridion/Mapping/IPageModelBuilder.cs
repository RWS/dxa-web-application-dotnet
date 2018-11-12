using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;
using Sdl.Web.DataModel;

namespace Sdl.Web.Tridion.Mapping
{
    /// <summary>
    /// Interface for Page Model Builders based on teh DXA R2 Data Model.
    /// </summary>
    /// <seealso cref="ModelBuilderPipeline"/>
    public interface IPageModelBuilder
    {
        /// <summary>
        /// Builds a strongly typed Page Model from a given DXA R2 Data Model.
        /// </summary>
        /// <param name="pageModel">The strongly typed Page Model to build. Is <c>null</c> for the first Page Model Builder in the pipeline.</param>
        /// <param name="pageModelData">The DXA R2 Data Model.</param>
        /// <param name="includePageRegions">Indicates whether Include Page Regions should be included.</param>
        /// <param name="localization">The context <see cref="ILocalization"/>.</param>
        void BuildPageModel(ref PageModel pageModel, PageModelData pageModelData, bool includePageRegions, Localization localization);
    }
}
