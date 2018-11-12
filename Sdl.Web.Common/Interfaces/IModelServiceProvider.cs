using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Navigation;
using Sdl.Web.DataModel;

namespace Sdl.Web.Common.Interfaces
{
    /// <summary>
    /// Interface for Model Service Provider extension point.
    /// </summary>
    public interface IModelServiceProvider
    {
        void AddDataModelExtension(IDataModelExtension extension);

        /// <summary>
        /// Returns the Page Model Data for a given path.
        /// </summary>
        /// <param name="urlPath">Path of page to request.</param>
        /// <param name="localization">The context Localization.</param>
        /// <param name="addIncludes">Add include pages.</param>
        /// <returns>Page Model Data</returns>
        PageModelData GetPageModelData(string urlPath, Localization localization, bool addIncludes);

        /// <summary>
        /// Returns the Page Model Data for a given page Id.
        /// </summary>
        /// <param name="pageId">Page Id.</param>
        /// <param name="localization">The context Localization.</param>
        /// <param name="addIncludes">Add include pages.</param>
        /// <returns>Page Model Data</returns>
        PageModelData GetPageModelData(int pageId, Localization localization, bool addIncludes);

        /// <summary>
        /// Gets the Entity Model Data given an entity id of the format {ComponentID}-{TemplateID}
        /// </summary>
        /// <param name="entityId">Entity Id.</param>
        /// <param name="localization">The context Localization.</param>
        /// <returns>Entity Model Data.</returns>
        EntityModelData GetEntityModelData(string entityId, Localization localization);

        /// <summary>
        /// Gets the Site map for a given localization.
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        /// <returns>Taxonomy Node</returns>
        TaxonomyNode GetSitemapItem(Localization localization);

        /// <summary>
        /// Gets the child site map items of a given parent site map item.
        /// </summary>
        /// <param name="parentSitemapItemId">Parent Id.</param>
        /// <param name="localization">The context Localization.</param>
        /// <param name="includeAncestors">Include Ancestors.</param>
        /// <param name="descendantLevels">Descendant Levels.</param>
        /// <returns></returns>
        SitemapItem[] GetChildSitemapItems(string parentSitemapItemId, Localization localization,
            bool includeAncestors, int descendantLevels);
    }
}
