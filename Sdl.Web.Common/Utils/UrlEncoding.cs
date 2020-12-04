using System;
using System.Text;

namespace Sdl.Web.Common.Utils
{
    /// <summary>
    /// Url encoding utilities.
    /// </summary>
    public static class UrlEncoding
    {
        /// <summary>
        /// Encode all non ascii characters.
        /// </summary>
        /// <param name="str">String to encode</param>
        /// <returns>Url encoded string</returns>
        public static string UrlEncodeNonAscii(string str)
        {
            return UrlEncodeNonAscii(str, Encoding.UTF8);
        }

        /// <summary>
        /// Encode all non ascii characters.
        /// </summary>
        /// <param name="str">String to encode</param>
        /// <param name="e"></param>
        /// <returns>Url encoded string</returns>
        public static string UrlEncodeNonAscii(string str, Encoding e)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            if (e == null) e = Encoding.UTF8;
            byte[] bytes = e.GetBytes(str);
            return Encoding.ASCII.GetString(UrlEncodeNonAscii(bytes));
        }

        /// <summary>
        /// Encode all non ascii characters.
        /// </summary>
        /// <param name="bytes">Data to encode</param>
        /// <returns>Url Encoded version</returns>
        public static byte[] UrlEncodeNonAscii(byte[] bytes)
        {
            int l = bytes.Length;
            byte[] buf = new byte[l * 3];
            int j = 0;
            for (int i = 0; i < l; i++)
            {
                byte b = bytes[i];
                if (b < 0x20 || b >= 0x7F)
                {
                    buf[j++] = 0x25;
                    buf[j++] = (byte)IntToHex((b >> 4) & 15);
                    buf[j++] = (byte)IntToHex(b & 15);
                }
                else
                {
                    buf[j++] = b;
                }
            }
            byte[] dst = new byte[j];
            Array.Copy(buf, dst, j);
            return dst;
        }

        internal static char IntToHex(int n) { return n <= 9 ? (char)(n + 0x30) : (char)((n - 10) + 0x61); }

        /// <summary>
        /// Performs an encoding of the url path. 
        /// </summary>
        /// <param name="url">Unencoded url path</param>
        /// <returns>Encoded url path.</returns>
        public static string UrlPartialPathEncode(string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            int length = url.Length;
            StringBuilder urlCopy = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                char c = url[i];
                switch (c)
                {
                    case ' ': urlCopy.Append("%20"); break;
                    case '!': urlCopy.Append("%21"); break;
                    case '"': urlCopy.Append("%22"); break;
                    case '#': urlCopy.Append("%23"); break;
                    case '$': urlCopy.Append("%24"); break;
                    case '&': urlCopy.Append("%26"); break;
                    case '\'': urlCopy.Append("%27"); break;
                    case '(': urlCopy.Append("%28"); break;
                    case ')': urlCopy.Append("%29"); break;
                    case '*': urlCopy.Append("%2A"); break;
                    case '+': urlCopy.Append("%2B"); break;
                    case ',': urlCopy.Append("%2C"); break;
                    case ':': urlCopy.Append("%3A"); break;
                    case ';': urlCopy.Append("%3B"); break;
                    case '<': urlCopy.Append("%3C"); break;
                    case '=': urlCopy.Append("%3D"); break;
                    case '>': urlCopy.Append("%3E"); break;
                    case '?': urlCopy.Append("%3F"); break;
                    case '@': urlCopy.Append("%40"); break;
                    case '[': urlCopy.Append("%5B"); break;
                    case ']': urlCopy.Append("%5D"); break;
                    case '^': urlCopy.Append("%5E"); break;
                    case '{': urlCopy.Append("%7B"); break;
                    case '|': urlCopy.Append("%7C"); break;
                    case '}': urlCopy.Append("%7D"); break;
                    case '~': urlCopy.Append("%7E"); break;
                    default: urlCopy.Append(c); break;
                }
            }

            return urlCopy.ToString();
        }

        /// <summary>
        /// Performs an encoding of the url path. 
        /// </summary>
        /// <param name="url">Unencoded url path</param>
        /// <returns>Encoded url path.</returns>
        public static string UrlPathEncode(string url) => string.IsNullOrEmpty(url) ? url : UrlPartialPathEncode(UrlEncodeNonAscii(url));
    }
}
