using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Sdl.Web.Tridion.Config
{
    /// <summary>
    /// Resolve URLs to localizations
    /// </summary>
    public class ConfigFileLocalizationResolver : ILocalizationResolver
    {
        private Dictionary<string, Localization> _localizations = null;
        private readonly object _localizationLock = new object();
        private const string _errorFormat = "Invalid cd_dynamic_conf.xml entry for publication {0}: Missing {1} element.";
        public Localization GetLocalizationFromUri(Uri uri)
        {
            if (_localizations == null)
            {
                LoadLocalizations();
            }
            string url = uri.ToString();
            foreach (var rootUrl in _localizations.Keys)
            {
                if (url.ToLower().StartsWith(rootUrl.ToLower()))
                {
                    var loc = _localizations[rootUrl];
                    if (loc.Culture == null)
                    {
                        loc = SiteConfiguration.LoadLocalization(loc);
                    }
                    Log.Debug("Request for {0} is from localization {1}", uri, loc.LocalizationId);
                    return loc;
                }
            }
            return null;
        }

        public Localization GetLocalizationFromId(string localizationId)
        {
            if (_localizations == null)
            {
                LoadLocalizations();
            }
            return _localizations.Values.Where(l => l.LocalizationId == localizationId).FirstOrDefault();
        }

        protected void LoadLocalizations()
        {
            lock (_localizationLock)
            {
                var rootApplicationFolder = AppDomain.CurrentDomain.BaseDirectory;
                _localizations = new Dictionary<string,Localization>();
                string path = Path.Combine(rootApplicationFolder, @"bin\config\cd_dynamic_conf.xml");
                XDocument config = XDocument.Load(path);
                // sorting publications by path in decending order so default path ("/" or "") is last in list
                var publications = config.Descendants("Publications").First();
                var sortedPublications = publications.Elements().OrderByDescending(e => e.Element("Host").Attribute("Path").Value);
                publications.ReplaceAll(sortedPublications);
                foreach (var pub in config.Descendants("Publication"))
                {
                    var loc = GetLocalization(pub.Element("Host"));
                    _localizations.Add(loc.GetBaseUrl(),loc);
                }
            }
        }

        protected Localization GetLocalization(XElement xElement)
        {
            var pubId = "";
            if (xElement.Parent.Attribute("Id")!=null)
            {
                pubId = xElement.Parent.Attribute("Id").Value;
            }
            else
            {
                var ex = new Exception("missing Id attribute on Host entry in cd_dynamic_conf.xml"); 
                Log.Error(ex);
                throw ex;
            }
            var loc = new Localization
            {
                Protocol = xElement.Attribute("Protocol") == null ? "http" : xElement.Attribute("Protocol").Value.ToLower(),
                Port = xElement.Attribute("Port") == null ? String.Empty : xElement.Attribute("Port").Value,
                LocalizationId = pubId
            };
            if (xElement.Attribute("Domain") != null)
            {
                loc.Domain = xElement.Attribute("Domain").Value.ToLower();
            }
            else
            {
                var ex = new Exception(String.Format(_errorFormat,pubId,"Domain"));
                Log.Error(ex);
                throw ex;
            }
            if (xElement.Attribute("Path") != null)
            {
                var path = xElement.Attribute("Path").Value;
                if (path!="/")
                {
                    loc.Path = path;
                }
            }
            if (loc.Path == null)
            {
                loc.Path = String.Empty;
            }   
            return loc;
        }
    }
}
