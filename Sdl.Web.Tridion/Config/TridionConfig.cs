using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using System.Xml.XPath;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Logging;
using Sdl.Web.Tridion.Markup;

namespace Sdl.Web.Tridion.Config
{
    /// <summary>
    /// Class to read Tridion related configuration
    /// </summary>
    public class TridionConfig
    {
        private static List<Dictionary<string,string>> _localizations;
        private static readonly Object LocalizationLock = new Object();
        private static readonly Object RegionLock = new Object();
        private static Dictionary<string, XpmRegion> _xpmRegions;

        // page title and meta field mappings
        public static string StandardMetadataXmlFieldName = "standardMeta";
        public static string StandardMetadataTitleXmlFieldName = "name";
        public static string StandardMetadataDescriptionXmlFieldName = "description";
        public static string RegionForPageTitleComponent = "Main";
        public static string ComponentXmlFieldNameForPageTitle = "headline";

        public static Dictionary<string, XpmRegion> XpmRegions
        {
            get
            {
                if (_xpmRegions == null)
                {
                    LoadRegions();
                }
                return _xpmRegions;
            }
        }
        
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

        public static void LoadLocalizations()
        {
            lock (LocalizationLock)
            {
                var rootApplicationFolder = AppDomain.CurrentDomain.BaseDirectory;
                _localizations = new List<Dictionary<string, string>>();
                string path = Path.Combine(rootApplicationFolder, @"bin\config\cd_dynamic_conf.xml");
                XDocument config = XDocument.Load(path);
                // sorting publications by path in decending order so default path ("/" or "") is last in list
                var publications = config.Descendants("Publications").First();
                var sortedPublications = publications.Elements().OrderByDescending(e => e.Element("Host").Attribute("Path").Value); 
                publications.ReplaceAll(sortedPublications);
                foreach (var pub in config.Descendants("Publication"))
                {
                    _localizations.Add(GetLocalization(pub.Element("Host")));
                }
            }
        }

        public static void LoadRegions()
        {
            lock (RegionLock)
            {
                var rootApplicationFolder = AppDomain.CurrentDomain.BaseDirectory;
                _xpmRegions = new Dictionary<string, XpmRegion>();
                var configPath = Path.Combine(new[] { rootApplicationFolder, SiteConfiguration.StaticsFolder, SiteConfiguration.DefaultLocalization.ToCombinePath(), @"system\mappings\regions.json" });
                foreach (var region in GetRegionsFromFile(configPath))
                {
                    _xpmRegions.Add(region.Region, region);
                }
            }
        }

        private static Dictionary<string, string> GetLocalization(XElement xElement)
        {
            var res = new Dictionary<string,string>();
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

        /// <summary>
        /// Gets a XPM region by name.
        /// </summary>
        /// <param name="name">The region name</param>
        /// <returns>The XPM region matching the name for the given module</returns>
        public static XpmRegion GetXpmRegion(string name)
        {
            return GetXpmRegion(XpmRegions, name);
        }

        private static XpmRegion GetXpmRegion(IReadOnlyDictionary<string, XpmRegion> regions, string name)
        {
            if (regions.ContainsKey(name))
            {
                return regions[name];
            }

            Exception ex = new Exception(string.Format("XPM Region '{0}' does not exist.", name));
            //TODO - do we throw an error, or apply some defaults?
            Log.Error(ex);
            throw ex;
        }

        private static IEnumerable<XpmRegion> GetRegionsFromFile(string file)
        {
            return new JavaScriptSerializer().Deserialize<List<XpmRegion>>(File.ReadAllText(file));
        }
    }
}
