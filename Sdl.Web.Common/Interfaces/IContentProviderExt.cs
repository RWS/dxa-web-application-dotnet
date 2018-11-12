using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Interfaces
{
    /// <summary>
    /// Extended Interface for Content Provider extension point.
    /// </summary>
    public interface IContentProviderExt : IContentProvider
    {       
        /// <summary>
        /// Gets a Page Model for a given Page Id.
        /// </summary>
        /// <param name="pageId">Page Id</param>
        /// <param name="localization">The context Localization.</param>
        /// <param name="addIncludes">Indicates whether include Pages should be expanded.</param>
        /// <returns>The Page Model.</returns>
        /// <exception cref="DxaItemNotFoundException">If no Page Model exists for the given Id.</exception>
        PageModel GetPageModel(int pageId, Localization localization, bool addIncludes = true);       
       
        /// <summary>
        /// Gets a Static Content Item (binary) for a given binary Id.
        /// </summary>
        /// <param name="binaryId">Id of binary.</param>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The Static Content Item.</returns>
        /// <exception cref="DxaItemNotFoundException">If no Static Content Item exists for the given URL.</exception>
        StaticContentItem GetStaticContentItem(int binaryId, Localization localization);
    }
}