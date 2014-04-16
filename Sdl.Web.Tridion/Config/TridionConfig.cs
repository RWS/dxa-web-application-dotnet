using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace Sdl.Web.Tridion
{
    /// <summary>
    /// Class to read Tridion cd_xxx_conf.xml configuration
    /// </summary>
    public class TridionConfig
    {
        private static List<Dictionary<string,string>> _localizations;
        private static object lock1 = new object();

        public static List<Dictionary<string, string>> PublicationMap
        {
            get
            {
                if (_localizations == null)
                {
                    LoadMappings();
                }
                return _localizations;
            }
        }

        public static void LoadMappings()
        {
            lock(lock1)
            {
                var rootApplicationFolder = HttpContext.Current.Server.MapPath("~/bin");
                _localizations = new List<Dictionary<string, string>>();
                XDocument config = XDocument.Load(rootApplicationFolder + "/config/cd_link_conf.xml");
                if (config != null)
                {
                    foreach (var pub in config.Descendants("Publication"))
                    {
                        _localizations.Add(GetLocalization(pub.Element("Host")));
                    }
                }
            }
        }

        private static Dictionary<string, string> GetLocalization(XElement xElement)
        {
            var res = new Dictionary<string,string>();
            res.Add("Protocol",xElement.Attribute("Protocol") == null ? "http" : xElement.Attribute("Protocol").Value);
            res.Add("Domain", xElement.Attribute("Domain") == null ? "no-domain-in-cd_link_conf" : xElement.Attribute("Domain").Value);
            res.Add("Port", xElement.Attribute("Port") == null ? "" : ":" + xElement.Attribute("Port").Value);
            res.Add("Path", (xElement.Attribute("Path") == null || xElement.Attribute("Path").Value == "/") ? "" : xElement.Attribute("Path").Value);
            res.Add("LocalizationId", xElement.Parent.Attribute("Id").Value);
            return res;
        }
    }
}
