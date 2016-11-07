using Sdl.Web.Common.Configuration;
using System;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Pseudo View Model respresenting a Redirection to a given URL.
    /// </summary>
    /// <remarks>
    /// A Controller's EnrichModel implementation can return this pseudo View Model in order to trigger an HTTP Redirect.
    /// </remarks>
    [Serializable]
    public class RedirectModel : ViewModel
    {
        public string RedirectUrl { get; private set; }

        public RedirectModel(string redirectUrl)
        {
            RedirectUrl = redirectUrl;
        }

        public override string GetXpmMarkup(Localization localization)
        {
            return null;
        }
    }
}