namespace Sdl.Web.Common.Extensions
{
    public static class StringExtensions
    {
        public static string RemoveSpaces(this string value)
        {
            return value.Replace(" ", string.Empty);
        }

        public static string ToCombinePath(this string value)
        {
            return value.Replace('/', '\\').Trim('\\');
        }
    }
}
