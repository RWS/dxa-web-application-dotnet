using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Interfaces
{
    /// <summary>
    /// Interface for Content Provider extension point.
    /// </summary>
    public interface IContentProvider
    {
        /// <summary>
        /// Gets a Page Model for a given URL path.
        /// </summary>
        /// <param name="urlPath">The URL path (unescaped).</param>
        /// <param name="localization">The context Localization.</param>
        /// <param name="addIncludes">Indicates whether include Pages should be expanded.</param>
        /// <returns>The Page Model.</returns>
        /// <exception cref="DxaItemNotFoundException">If no Page Model exists for the given URL.</exception>
        PageModel GetPageModel(string urlPath, ILocalization localization, bool addIncludes = true);

        /// <summary>
        /// Gets a Page Model for a given Page Id.
        /// </summary>
        /// <param name="pageId">Page Id</param>
        /// <param name="localization">The context Localization.</param>
        /// <param name="addIncludes">Indicates whether include Pages should be expanded.</param>
        /// <returns>The Page Model.</returns>
        /// <exception cref="DxaItemNotFoundException">If no Page Model exists for the given Id.</exception>
        PageModel GetPageModel(int pageId, ILocalization localization, bool addIncludes = true);

        /// <summary>
        /// Gets an Entity Model for a given Entity Identifier.
        /// </summary>
        /// <param name="id">The Entity Identifier. Must be in format {ComponentID}-{TemplateID}.</param>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The Entity Model.</returns>
        /// <exception cref="DxaItemNotFoundException">If no Entity Model exists for the given URL.</exception>
        EntityModel GetEntityModel(string id, ILocalization localization);

        /// <summary>
        /// Gets a Static Content Item (binary) for a given URL path.
        /// </summary>
        /// <param name="urlPath">The URL path (unescaped).</param>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The Static Content Item.</returns>
        /// <exception cref="DxaItemNotFoundException">If no Static Content Item exists for the given URL.</exception>
        StaticContentItem GetStaticContentItem(string urlPath, ILocalization localization);

        /// <summary>
        /// Gets a Static Content Item (binary) for a given binary Id.
        /// </summary>
        /// <param name="binaryId">Id of binary.</param>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The Static Content Item.</returns>
        /// <exception cref="DxaItemNotFoundException">If no Static Content Item exists for the given URL.</exception>
        StaticContentItem GetStaticContentItem(int binaryId, ILocalization localization);

        /// <summary>
        /// Populates a Dynamic List by executing the query it specifies.
        /// </summary>
        /// <param name="dynamicList">The Dynamic List which specifies the query and is to be populated.</param>
        /// <param name="localization">The context Localization.</param>
        void PopulateDynamicList(DynamicList dynamicList, ILocalization localization);
    }
}