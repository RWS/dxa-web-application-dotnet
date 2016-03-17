using System.Web;

namespace Sdl.Web.Common.Models
{

    /// <summary>
    /// Represents the View Model for a Page with additional HTTP Response data (cookies, headers).
    /// </summary>
    public abstract class PageModelWithHttpResponseData : PageModel
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PageModelWithHttpResponseData"/>.
        /// </summary>
        /// <param name="id">The identifier of the Page.</param>
        protected PageModelWithHttpResponseData(string id)
            : base(id)
        {
        }

        /// <summary>
        /// Sets the HTTP Response data (cookies, header).
        /// </summary>
        /// <param name="httpResponse">The HTTP Response to set the data on.</param>
        /// <remarks>
        /// This should only be used to set HTTP headers (incl. cookies); setting the HTTP response body is a responsibility of the Page Controller.
        /// This method is called by the DXA Page Controller before rendering the body.
        /// </remarks>
        public abstract void SetHttpResponseData(HttpResponse httpResponse);
    }
}
