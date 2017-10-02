using DD4T.ContentModel;
using DD4T.ContentModel.Contracts.Serializing;
using Microsoft.Xml.Serialization.GeneratedAssembly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace DD4T.Serialization
{
    public class XmlSerializerService : BaseSerializerService
    {
        private static Dictionary<Type, XmlSerializer> _xmlSerializers = new Dictionary<Type, XmlSerializer>();

        private XmlSerializer GetXmlSerializer<T>() where T : XmlSerializer
        {
            if (!_xmlSerializers.ContainsKey(typeof(T)))
            {
                XmlSerializer serializer = (T)Activator.CreateInstance(typeof(T));
                _xmlSerializers.Add(typeof(T), serializer);
            }
            return _xmlSerializers[typeof(T)];
        }

        private string Serialize(object o, XmlSerializer serializer)
        {
            StringWriter sw = new StringWriter();
            MemoryStream ms = new MemoryStream();
            XmlWriter writer = new XmlTextWriterFormattedNoDeclaration(ms, Encoding.UTF8);
            string outputValue;
            //Create our own namespaces for the output
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();

            //Add an empty namespace and empty value
            ns.Add("", "");

            serializer.Serialize(writer, o, ns);
            outputValue = Encoding.UTF8.GetString(ms.ToArray());

            // for some reason, the .NET serializer leaves an invalid character at the start of the string
            // we will remove everything up to the first < so that the XML can be deserialized later!
            Regex re = new Regex("^[^<]+");
            outputValue = re.Replace(outputValue, "");
            return outputValue;
        }

        public override string Serialize<T>(T input)
        {
            string result;
            if (input is Page || input is IPage)
            {
                result = Serialize(input, GetXmlSerializer<PageSerializer>());
            }
            else if (input is Component || input is IComponent)
            {
                result = Serialize(input, GetXmlSerializer<ComponentSerializer>());
            }
            else if (input is ComponentPresentation || input is IComponentPresentation)
            {
                result = Serialize(input, GetXmlSerializer<ComponentPresentationSerializer>());
            }
            else
            {
                throw new Exception("cannot serialize object of type " + typeof(T));
            }
            return ((SerializationProperties)SerializationProperties).CompressionEnabled ? Compressor.Compress(result) : result;
        }

        public override T Deserialize<T>(string input)
        {
            // NOTE: important exception situation!!
            // if the requested type is IComponentPresentation, there is a possiblity that the data
            // provided to us actually contains a Component instead. In that case we need to add a
            // dummy CT / CP around the Component and return that!

            if (((SerializationProperties)SerializationProperties).CompressionEnabled)
            {
                input = Compressor.Decompress(input);
            }

            TextReader tr = new StringReader(input);

            XmlSerializer serializer = null;

            if (typeof(T) == typeof(Page) || typeof(T) == typeof(IPage))
            {
                serializer = GetXmlSerializer<PageSerializer>();
            }
            else if (typeof(T) == typeof(Component) || typeof(T) == typeof(IComponent))
            {
                serializer = GetXmlSerializer<ComponentSerializer>();
            }
            else if (typeof(T) == typeof(ComponentPresentation) || typeof(T) == typeof(IComponentPresentation))
            {
                if (typeof(T).Name.Contains("ComponentPresentation")
                    && !input.Substring(0, 30).ToLower().Contains("componentpresentation"))
                {
                    // handle the exception situation where we are asked to deserialize into a CP but the data is actually a Component
                    serializer = GetXmlSerializer<ComponentSerializer>();
                    Component component = (Component)serializer.Deserialize(tr);
                    IComponentPresentation componentPresentation = new ComponentPresentation()
                    {
                        Component = component,
                        ComponentTemplate = new ComponentTemplate()
                    };
                    return (T)componentPresentation;
                }
                serializer = GetXmlSerializer<ComponentPresentationSerializer>();
            }
            return (T)serializer.Deserialize(tr);
        }

        public override bool IsAvailable()
        {
            return Type.GetType("Microsoft.Xml.Serialization.GeneratedAssembly.ComponentSerializer") != null;
        }
    }

    public class XmlTextWriterFormattedNoDeclaration : XmlTextWriter
    {
        public XmlTextWriterFormattedNoDeclaration(System.IO.TextWriter w)
            : base(w)
        {
            Formatting = System.Xml.Formatting.Indented;
        }

        public XmlTextWriterFormattedNoDeclaration(System.IO.MemoryStream ms, Encoding enc)
            : base(ms, enc)
        {
            Formatting = System.Xml.Formatting.Indented;
        }

        public override void WriteStartDocument()
        {
        } // suppress
    }
}