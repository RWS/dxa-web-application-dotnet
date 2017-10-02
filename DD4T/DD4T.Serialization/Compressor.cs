using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace DD4T.Serialization
{
    public class Compressor
    {
        public static string Compress(string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    msi.CopyTo(gs);
                }

                return Convert.ToBase64String(mso.ToArray());
            }
        }

        public static string Decompress(string s)
        {
            var bytes = Convert.FromBase64String(s);
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    gs.CopyTo(mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }
    }
}
