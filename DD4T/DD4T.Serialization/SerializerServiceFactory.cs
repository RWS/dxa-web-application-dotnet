using DD4T.ContentModel.Contracts.Serializing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DD4T.Serialization
{
    public class SerializerServiceFactory
    {
        private static Dictionary<Regex, string> serializersByPattern = new Dictionary<Regex, string>()
        {
            { new Regex ("^<"), "DD4T.Serialization.XmlSerializerService" },
            { new Regex ("^{"), "DD4T.Serialization.JSONSerializerService" }
        };

        private static Dictionary<string, BaseSerializerService> serializerObjectsByPattern = new Dictionary<string, BaseSerializerService>();

        private static string defaultSerializerServerType = "DD4T.Serialization.JSONSerializerService";

        public static ISerializerService FindSerializerServiceForContent(string content)
        {
            // first trim leading and trailing whitespace 
            string contentToCheck = content.Trim();
            bool isCompressed = false;
            foreach (Regex re in serializersByPattern.Keys)
            {
                if (re.IsMatch(contentToCheck))
                {
                    return Initialize(serializersByPattern[re], isCompressed);
                }
            }
            // content is probably compressed, try uncompressing it now
            // first, try to decompress (this will fail if the content is uncompressed, of course)
           
            try
            {
                content = Compressor.Decompress(content);
                isCompressed = true;
            }
            catch
            {
                // content is apparently not compressed, and it does not match any of the 
                // known start strings, so we will throw this exception
                throw;
            }

            contentToCheck = content.Trim();
            foreach (Regex re in serializersByPattern.Keys)
            {
                if (re.IsMatch(contentToCheck))
                {
                    return Initialize(serializersByPattern[re], isCompressed);
                }
            }

            return Initialize(defaultSerializerServerType, isCompressed);
        }

        static object o = new object();
        private static BaseSerializerService Initialize(string typeName, bool isCompressed)
        {
            string key = string.Format("{0}_{1}", typeName, isCompressed);
            if (!serializerObjectsByPattern.ContainsKey(key))
            {
                lock (o)
                {
                    if (!serializerObjectsByPattern.ContainsKey(key))
                    {
                        Type t = Type.GetType(typeName);
                        BaseSerializerService service = Activator.CreateInstance(t) as BaseSerializerService;
                        if (isCompressed)
                        {
                            service.SerializationProperties = new SerializationProperties() { CompressionEnabled = true };
                        }
                        serializerObjectsByPattern.Add(key, service);

                    }
                }
            }
            return serializerObjectsByPattern[key];
        }
    }
}
