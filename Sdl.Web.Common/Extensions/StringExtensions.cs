using System;
namespace Sdl.Web.Common.Extensions
{
    public static class StringExtensions
    {
        public static string RemoveSpaces(this string value)
        {
            return value.Replace(" ", string.Empty);
        }

        public static string ToCombinePath(this string value, bool prefixWithSlash = false)
        {
            return (prefixWithSlash ? "\\" : "") + value.Replace('/', '\\').Trim('\\');
        }

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
    }
}
