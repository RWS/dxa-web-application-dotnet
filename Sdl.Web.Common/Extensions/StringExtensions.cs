using System;
using System.Text.RegularExpressions;

namespace Sdl.Web.Common.Extensions
{
    public static class StringExtensions
    {
        // Regex to identify a valid cm identifier
        private static readonly Regex CmUriRegEx = new Regex(@"^(tcm|ish):\d+-\d+(-\d+){0,2}$", RegexOptions.Compiled);

        /// <summary>
        /// Remove spaces from string.
        /// </summary>
        public static string RemoveSpaces(this string value) => value.Replace(" ", string.Empty);

        /// <summary>
        /// Converts string to path friendly string.
        /// </summary>
        public static string ToCombinePath(this string value, bool prefixWithSlash = false) => (prefixWithSlash ? "\\" : "") + value.Replace('/', '\\').Trim('\\');

        /// <summary>
        /// Returns true if a string contains N or more characters 'c'.
        /// </summary>
        public static bool HasNOrMoreOccurancesOfChar(this string str, int n, char c)
        {
            int count = 0;
            for (int i = 0; i < str.Length || count >= n; i++)
            {
                if (str[i] == c)
                {
                    count++;
                }
            }
            return count >= n;
        }

        /// <summary>
        /// Returns a string converted to camel case.
        /// </summary>
        public static string ToCamelCase(this string str) => str.Substring(0, 1).ToLower() + str.Substring(1);

        /// <summary>
        /// Returns a new string in which all occurances of a specifid string are replaced with a new string
        /// using the provided string comparison option
        /// </summary>
        /// <param name="str">Original string</param>
        /// <param name="oldValue">Value to replace</param>
        /// <param name="newValue">Replacement</param>
        /// <param name="comparisonType">String comparison</param>
        /// <returns></returns>
        public static string Replace(this string str, string oldValue, string newValue, StringComparison comparisonType)
        {
            if (str == null || oldValue == null || newValue == null) return str;
            int n = oldValue.Length;
            string lhs = "";
            string rhs = str;
            while (true)
            {   // go through looking for all matches of oldValue and replace
                int index = rhs.IndexOf(oldValue, comparisonType);
                if (index == -1) break;
                string tmp = rhs.Substring(index + n);
                lhs = lhs + rhs.Substring(0, index) + newValue;
                rhs = tmp;
            }
            return lhs + rhs;
        }

        /// <summary>
        /// Normalizes a URL path for a Page.
        /// </summary>
        /// <remarks>
        /// The following normalization actions are taken:
        /// <list type="bullet">
        ///     <item>Ensure the URL path is extensionless.</item>
        ///     <item>Ensure the URL path for an index page ends with "/index".</item>
        /// </list>
        /// </remarks>
        /// <param name="urlPath">The input URL path (the subject for this extension method).</param>
        /// <returns>The normalized URL path.</returns>
        public static string NormalizePageUrlPath(this string urlPath)
        {
            if (urlPath == null)
            {
                return null;
            }
            if (urlPath.EndsWith(Constants.DefaultExtension))
            {
                urlPath = urlPath.Substring(0, urlPath.Length - Constants.DefaultExtension.Length);
            }
            if (urlPath.EndsWith("/"))
            {
                urlPath += Constants.DefaultExtensionLessPageName;
            }
            return urlPath;
        }

        /// <summary>
        /// Determins if a string is CM identifier (Tcm uri).
        /// </summary>
        /// <param name="str">String to check</param>
        /// <returns>True if a valid CM identifier</returns>
        public static bool IsCmIdentifier(this string str) => CmUriRegEx.IsMatch(str);
    }
}
