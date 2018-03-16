using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Extensions
{
    public static class LinkExtensions
    {
        /// <summary>
        /// Returns true if the link is valid.
        /// </summary>
        public static bool IsValidLink(this Link link) => !string.IsNullOrEmpty(link?.Url);

        /// <summary>
        /// Returns true if the link contains a url and link text.
        /// </summary>
        public static bool IsLinkWithText(this Link link) => !string.IsNullOrEmpty(link?.Url) && !string.IsNullOrEmpty(link.LinkText);
    }
}
