using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Extensions
{
    public static class LinkExtensions
    {
        public static bool IsValidLink(this Link link)
        {
            return !string.IsNullOrEmpty(link?.Url);
        }

        public static bool IsLinkWithText(this Link link)
        {
            return !string.IsNullOrEmpty(link?.Url) && !string.IsNullOrEmpty(link.LinkText);
        }
    }
}
