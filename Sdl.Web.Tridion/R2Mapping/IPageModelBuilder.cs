using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;
using Sdl.Web.DataModel;

namespace Sdl.Web.Tridion.R2Mapping
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
        /// <param name="pageModelData">The DXA R2 Data Model.</param>
        /// <param name="includePageRegions">Indicates whether Include Page Regions should be included.</param>
        /// <param name="inputPageModel">Strongly typed Page Model created by preceding Page Model Builder; is <c>null</c> for the first Page Model Builder in the chain.</param>
        /// <param name="localization">The context <see cref="Localization"/>.</param>
        /// <returns>The strongly typed Page Model.</returns>
        PageModel BuildPageModel(PageModelData pageModelData, bool includePageRegions, PageModel inputPageModel, Localization localization);
    }
}
