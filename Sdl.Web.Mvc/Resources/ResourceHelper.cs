using System.IO;
using System.Reflection;

namespace Sdl.Web.Mvc.Resources
{
    public static class ResourceHelper
    {
        /// <summary>
        /// Load embedded resource from calling assembly
        /// </summary>
        /// <param name="name">Fully qualified name of the resource, e.g. Sdl.Web.Mvc.Resources.rss.xslt</param>
        /// <returns>Embedded resource as a string</returns>
        public static string GetEmbeddedResource(string name)
        {
            return GetEmbeddedResource(name, Assembly.GetCallingAssembly());
        }

        /// <summary>
        /// Load embedded resource from calling assembly
        /// </summary>
        /// <param name="name">Fully qualified name of the resource, e.g. Sdl.Web.Mvc.Resources.rss.xslt</param>
        /// <returns>Embedded resource as a Stream</returns>
        public static Stream GetEmbeddedResourceAsStream(string name)
        {
            return GetEmbeddedResourceAsStream(name, Assembly.GetCallingAssembly());
        }

        private static string GetEmbeddedResource(string name, Assembly assembly)
        {
            using (Stream stream = GetEmbeddedResourceAsStream(name, assembly))
            {
                if (stream == null)
                {
                    return null;
                }

                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private static Stream GetEmbeddedResourceAsStream(string name, Assembly assembly)
        {
            return assembly.GetManifestResourceStream(name);
        }
    }
}
