using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Sdl.Web.Tridion.Config
{
    /// <summary>
    /// Class to read Tridion related configuration
    /// </summary>
    public class TridionConfig
    {
        private static List<Dictionary<string,string>> _localizations;
        private static readonly Object LocalizationLock = new Object();

        // page title and meta field mappings
        public static string StandardMetadataXmlFieldName = "standardMeta";
        public static string StandardMetadataTitleXmlFieldName = "name";
        public static string StandardMetadataDescriptionXmlFieldName = "description";
        public static string RegionForPageTitleComponent = "Main";
        public static string ComponentXmlFieldNameForPageTitle = "headline";

        public static List<Dictionary<string, string>> PublicationMap
        {
            get
            {
                if (_localizations == null)
                {
                    LoadLocalizations();
                }
                return _localizations;
            }
        }

        private static void LoadLocalizations()
        {
            lock (LocalizationLock)
            {
                string rootApplicationFolder = AppDomain.CurrentDomain.BaseDirectory;
                _localizations = new List<Dictionary<string, string>>();
                string path = Path.Combine(rootApplicationFolder, @"bin\config\cd_dynamic_conf.xml");
                XDocument config = XDocument.Load(path);
                // sorting Publications by Path in decending order so default Path ("/" or "") is last in list
                // using Path of first Host element found in a Publication, assuming the Paths of all of these Host elements will be equal
                XElement publications = config.Descendants("Publications").First();
                IOrderedEnumerable<XElement> sortedPublications = publications.Elements().OrderByDescending(e => e.Element("Host").Attribute("Path").Value); 
                publications.ReplaceAll(sortedPublications);
                foreach (XElement pub in config.Descendants("Publication"))
                {
                    // there could be multiple Host elements per Publication, add them all
                    foreach (XElement host in pub.Elements("Host"))
                    {
                        _localizations.Add(GetLocalization(host));
                    }
                }
            }
        }

        private static Dictionary<string, string> GetLocalization(XElement xElement)
        {
            Dictionary<string, string> res = new Dictionary<string,string>();
            if (xElement.Attribute("Protocol") != null)
            {
                res.Add("Protocol", xElement.Attribute("Protocol").Value);
            }
            if (xElement.Attribute("Domain") != null)
            {
                // change to lowercase, since that is how we expect it
                res.Add("Domain", xElement.Attribute("Domain").Value.ToLower());
            }
            if (xElement.Attribute("Port") != null)
            {
                res.Add("Port", xElement.Attribute("Port").Value);
            }
            if (xElement.Attribute("Path") != null)
            {
                res.Add("Path", xElement.Attribute("Path").Value);
            }
            res.Add("LocalizationId", xElement.Parent.Attribute("Id").Value);
            return res;
        }
    }
}
