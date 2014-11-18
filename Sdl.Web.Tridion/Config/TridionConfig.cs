using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Logging;
using Sdl.Web.Tridion.Markup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
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

        private const string RegionSettingsType = "regions";
        private static readonly Dictionary<string, Dictionary<string, XpmRegion>> XpmRegions = new Dictionary<string,Dictionary<string,XpmRegion>>();

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
        /// <param name="loc"></param>
        /// <returns>The XPM region matching the name for the given module</returns>
        public static XpmRegion GetXpmRegion(string name, Localization loc)
        {
            var key = loc.LocalizationId;
            if (!XpmRegions.ContainsKey(key) || SiteConfiguration.CheckSettingsNeedRefresh(RegionSettingsType,loc.LocalizationId))
            {
                LoadRegionsForLocalization(loc);
            }
            if (XpmRegions.ContainsKey(key))
            {
                var regionData = XpmRegions[key];
                if (regionData.ContainsKey(name))
                {
                    return regionData[name];
                }
            }
            Log.Warn("XPM Region '{0}' does not exist in localization {1}.", name, loc.LocalizationId);
            return null;
        }

        private static void LoadRegionsForLocalization(Localization loc)
        {
            var key = loc.LocalizationId;
            var url = Path.Combine(loc.Path.ToCombinePath(true), @"system\mappings\regions.json").Replace("\\", "/");
            var jsonData = SiteConfiguration.StaticFileManager.Serialize(url, loc, true);
            if (jsonData != null)
            {
                var regions = new Dictionary<string, XpmRegion>();
                foreach (var region in GetRegionsFromFile(jsonData))
                {
                    regions.Add(region.Region, region);
                }
                SiteConfiguration.ThreadSafeSettingsUpdate(RegionSettingsType, XpmRegions, key, regions);
            }
            else
            {
                Log.Error("Region file: {0} does not exist for localization {1}. Check that the Publish Settings page has been published in this publication.", url, loc.LocalizationId);
            }
        }
        
        private static IEnumerable<XpmRegion> GetRegionsFromFile(string jsonData)
        {
            return new JavaScriptSerializer().Deserialize<List<XpmRegion>>(jsonData);
        }
    }
}
