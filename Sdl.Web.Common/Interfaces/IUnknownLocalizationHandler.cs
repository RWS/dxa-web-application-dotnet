using System.Web;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Common.Interfaces
{
    /// <summary>
    /// Interface for Unknown Localization Handler extension point.
    /// </summary>
    /// <remarks>
    /// This extension points allows an implementation to intercept request for an Unknown Localization and optionally provide a different HTTP Response
    /// than the default HTTP 404 with a plain-text error message.
    /// </remarks>
    public interface IUnknownLocalizationHandler
    {
        /// <summary>
        /// Handles a Request for an Unknown Localization (i.e. the request URL doesn't map to a Publication).
        /// </summary>
        /// <param name="exception">The <see cref="DxaUnknownLocalizationException"/> exception.</param>
        /// <param name="request">The HTTP Request.</param>
        /// <param name="response">The HTTP Response. In order to return a different HTTP Response than the default, 
        /// the response headers and body should be set and <see cref="HttpResponse.End"/> should be called to terminate the HTTP processing pipeline.
        /// </param>
        /// <returns>May return a <see cref="ILocalization"/> instance if the handler manages to resolve the Localization. If <c>null</c> is returned, default error handling will be applied.</returns>
        Localization HandleUnknownLocalization(DxaUnknownLocalizationException exception, HttpRequest request, HttpResponse response);
    }
}
