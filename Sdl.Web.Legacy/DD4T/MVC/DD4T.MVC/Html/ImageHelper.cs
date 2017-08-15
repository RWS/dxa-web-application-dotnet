using System.IO;

namespace DD4T.Mvc.Html
{
    public static class ImageHelper
    {
        public static string ResizeToWidth(this string url, int width)
        {
            return ResizeToDimension(url, width, 'w');
        }

        public static string ResizeToHeight(this string url, int height)
        {
            return ResizeToDimension(url, height, 'h');
        }

        public static string ResizeToWidthAndHeight(this string url, int width, int height)
        {
            return ResizeToDimension(ResizeToDimension(url, width, 'w'), height, 'h');
        }

        private static string ResizeToDimension(string url, int val, char dimension)
        {
            string extension = Path.GetExtension(url);

            if (!string.IsNullOrEmpty(extension))
                return url.Replace(extension, "_" + dimension + val.ToString() + extension);

            return url;
        }
    }
}
