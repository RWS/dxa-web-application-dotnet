using System;
using System.Text.RegularExpressions;

namespace Sdl.Web.Tridion.Extensions
{
    public static class StringExtensions
    {
        public static string StripIllegalCharacters(this string value, string regex)
        {
            // replace invalid characters with empty strings
            try
            {
                return Regex.Replace(value, regex, string.Empty, RegexOptions.None, TimeSpan.FromSeconds(1));
            }
            // if we timeout when replacing invalid characters, we should return Empty
            catch (RegexMatchTimeoutException)
            {
                return string.Empty;
            }
        }
    }
}
