using System;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Extensions
{
    public static class LinkExtensions
    {
        public static bool IsValidLink(this Link link)
        {
            return link != null && !String.IsNullOrEmpty(link.Url);
        }

        public static bool IsLinkWithText(this Link link)
        {
            return link != null && !String.IsNullOrEmpty(link.Url) && !String.IsNullOrEmpty(link.LinkText);
        }
    }
}
