using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Interfaces
{
    /// <summary>
    /// Interface for Content Provider extension point.
    /// </summary>
    /// <remarks>
    /// Although this interface existed in STRI 1.0, it is not compatible in DXA 1.1.
    /// <list type="bullet">
    ///     <item><see cref="GetPageModel"/> and <see cref="GetEntityModel"/> now returned strongly typed results (DXA View Models).</item>
    ///     <item>All methods now have a parameter to explicitly pass in the context <see cref="Localization"/>.</item>
    ///     <item>GetPageContent and GetEntityContent have been removed; these would leak the underlying data representation.</item>
    ///     <item>GetNavigationModel has been moved to a separate <see cref="INavigationProvider"/> interface.</item>
    ///     <item><see cref="GetStaticContentItem"/> method has been added.</item>
    ///     <Item><see cref="ContentResolver"/> property has been deprecated, because <see cref="IContentResolver"/> is deprecated 
    ///         and the new extension points can be accessed through <see cref="Sdl.Web.Common.Configuration.SiteConfiguration"/>.</Item>
    ///     <Item><see cref="PopulateDynamicList"/> no longer returns a value; the Content List provided as parameter is populated.</Item>
    /// </list>
    /// </remarks>
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
        PageModel GetPageModel(string urlPath, Localization localization, bool addIncludes = true);

        /// <summary>
        /// Gets an Entity Model for a given Entity Identifier.
        /// </summary>
        /// <param name="id">The Entity Identifier. Must be in format {ComponentID}-{TemplateID}.</param>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The Entity Model.</returns>
        /// <exception cref="DxaItemNotFoundException">If no Entity Model exists for the given URL.</exception>
        EntityModel GetEntityModel(string id, Localization localization);

        /// <summary>
        /// Gets a Static Content Item (binary) for a given URL path.
        /// </summary>
        /// <param name="urlPath">The URL path (unescaped).</param>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The Static Content Item.</returns>
        /// <exception cref="DxaItemNotFoundException">If no Static Content Item exists for the given URL.</exception>
        StaticContentItem GetStaticContentItem(string urlPath, Localization localization);

        /// <summary>
        /// Populates a Dynamic List by executing the query it specifies.
        /// </summary>
        /// <param name="dynamicList">The Dynamic List which specifies the query and is to be populated.</param>
        /// <param name="localization">The context Localization.</param>
        void PopulateDynamicList(DynamicList dynamicList, Localization localization);
    }
}
