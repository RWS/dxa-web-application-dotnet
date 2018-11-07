using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;

namespace Sdl.Web.Mvc.Html
{
    /// <summary>
    /// Interface for Markup Decorator extension point.
    /// </summary>
    /// <remarks>
    /// Markup Decorators can be used to post-process the HTML rendered by Entity or Region Views.
    /// They are invoked by the HtmlHelperExtensions.DxaEntity and HtmlHelperExtensions.DxaRegion helper methods.
    /// They are registered in Area Registration code uusing the <see cref="BaseAreaRegistration.RegisterMarkupDecorator"/> method.
    /// </remarks>
    public interface IMarkupDecorator
    {
        /// <summary>
        /// Decorates the HTML markup rendered by a Entity or Region View.
        /// </summary>
        /// <param name="htmlToDecorate">The HTML to decorate.</param>
        /// <param name="viewModel">The <see cref="ViewModel"/> associated with the HTML fragment.</param>
        /// <returns>The decorated HTML.</returns>
        string DecorateMarkup(string htmlToDecorate, ViewModel viewModel);
    }
}
