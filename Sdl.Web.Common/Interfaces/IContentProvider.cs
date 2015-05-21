using System;
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
    ///     <item>GetPageContent and GetEntityContent have been removed; these would leak the underlying data representation.</item>
    ///     <item>GetNavigationModel has been moved to a separate <see cref="INavigationProvider"/> interface.</item>
    ///     <Item><see cref="ContentResolver"/> property has been deprecated, because <see cref="IContentResolver"/> is deprecated 
    ///         and the new extension points can be accessed through <see cref="Sdl.Web.Common.Configuration.SiteConfiguration"/>.</Item>
    ///     <Item><see cref="PopulateDynamicList"/> no longer returns a value; the Content List provided as parameter is populated.</Item>
    /// </list>
    /// </remarks>
    public interface IContentProvider
    {
        [Obsolete("Deprecated in DXA 1.1. Use SiteConfiguration.LinkResolver or SiteConfiguration.RichTextProcessor to get the new extension points.")]
        IContentResolver ContentResolver { get; set; }

        /// <summary>
        /// Gets a Page Model for a given URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="addIncludes">Indicates whether include Pages should be expanded.</param>
        /// <returns>The Page Model.</returns>
        PageModel GetPageModel(string url, bool addIncludes = true);

        /// <summary>
        /// Gets an Entity Model for a given Entity Identifier.
        /// </summary>
        /// <param name="id">The Entity Identifier.</param>
        /// <returns>The Entity Model.</returns>
        EntityModel GetEntityModel(string id);

        /// <summary>
        /// Populates a Content List by executing the query it specifies.
        /// </summary>
        /// <param name="contentList">The Content List which specifies the query and is to be populated.</param>
        void PopulateDynamicList<T>(ContentList<T> contentList) where T: EntityModel; 
    }
}