using Sdl.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace Sdl.Web.Tridion
{
    public class TridionConfig
    {
        private static Dictionary<string, Localization> _localizations;
        private static object lock1 = new object();

        public static Dictionary<string, Localization> LocalizationMap
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
                _localizations = new Dictionary<string, Localization>();
                XDocument config = XDocument.Load(rootApplicationFolder + "/config/cd_link_conf.xml");
                if (config != null)
                {
                    foreach (var pub in config.Descendants("Publication"))
                    {
                        var localization = GetLocalization(pub.Element("Host"));
                        _localizations.Add(localization.GetBaseUrl(), localization);
                    }
                }
            }
        }

        private static Localization GetLocalization(XElement xElement)
        {
            var res = new Localization();
            res.Protocol = xElement.Attribute("Protocol") == null ? "http" : xElement.Attribute("Protocol").Value;
            res.Domain = xElement.Attribute("Domain") == null ? "no-domain-in-cd_link_conf" : xElement.Attribute("Domain").Value;
            res.Port = xElement.Attribute("Port") == null ? "" : ":" + xElement.Attribute("Port").Value;
            res.Path = (xElement.Attribute("Path") == null || xElement.Attribute("Path").Value == "/") ? "" : xElement.Attribute("Path").Value;
            res.LocalizationId = Int32.Parse(xElement.Parent.Attribute("Id").Value);
            return res;
        }

        public static int GetPublicationIdFromUrl(Uri uri)
        {
            if (LocalizationMap.Count == 0)
            {
                return 0;
            }
            else
            {
                return GetLocalizationFromUrl(uri).LocalizationId;
            }
        }

        public static Localization GetLocalizationFromUrl(Uri uri)
        {
            //If theres a single publication use that regardless
            if (_localizations.Count==1)
            {
                return _localizations.SingleOrDefault().Value;
            }
            Localization res = null;
            foreach(var key in _localizations.Keys)
            {
                if (uri.AbsoluteUri.StartsWith(key))
                {
                    return _localizations[key];
                }
            }
            if (res == null)
            {
                //Throw an exception if there are multiple publication nodes, but we do not have a match
                throw new Exception (String.Format("No publication configuration found in cd_link_conf.xml for uri {0}",uri.ToString()));
            }
            return res;
        }
    }
}
