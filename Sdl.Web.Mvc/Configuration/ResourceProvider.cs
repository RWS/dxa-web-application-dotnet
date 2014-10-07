using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Web.Compilation;
using System.Web.Helpers;
using System.Web.Script.Serialization;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Logging;

namespace Sdl.Web.Mvc.Configuration
{
    /// <summary>
    /// Resource Provider to read resources from JSON files on disk
    /// </summary>
    public class ResourceProvider : IResourceProvider
    {
        private static Dictionary<string, Dictionary<string, object>> _resources;
        private static readonly object ResourceLock = new object();
        public static DateTime LastSettingsRefresh { get; set; }
        public object GetObject(string resourceKey, CultureInfo culture)
        {
            //Ignore the culture - we read this from the RequestContext
            var dictionary = GetResourceCache();
            if (!dictionary.Contains(resourceKey))
            {
                //default is to return resource key, to aid troubleshooting
                return resourceKey;
            }
            return dictionary[resourceKey];
        }

        public IResourceReader ResourceReader
        {
            get { return new ResourceReader(GetResourceCache()); }
        }

        private IDictionary GetResourceCache()
        {
            return Resources(WebRequestContext.Localization);
        }

        public IDictionary Resources(Localization localization)
        {
            if (_resources == null || LastSettingsRefresh < SiteConfiguration.LastSettingsRefresh)
            {
                //TODO only invalidate part of resources on refresh
                _resources = new Dictionary<string, Dictionary<string, object>>();
                LastSettingsRefresh = DateTime.Now;
            }
            if (!_resources.ContainsKey(localization.LocalizationId))
            {
                LoadResourcesForLocalization(localization);
                if (!_resources.ContainsKey(localization.LocalizationId))
                {
                    var ex = new Exception(String.Format("No resources can be found for localization {0}. Check that the localization path is correct and the resources have been published.", localization.GetBaseUrl()));
                    Log.Error(ex);
                    throw ex;
                }
            }
            return _resources[localization.LocalizationId];
        }

        private static void LoadResourcesForLocalization(Localization loc)
        {
            if (!_resources.ContainsKey(loc.LocalizationId))
            {
                var applicationRoot = AppDomain.CurrentDomain.BaseDirectory;
                Log.Debug("Loading resources for localization : '{0}'", loc.GetBaseUrl());
                var resources = new Dictionary<string, object>();
                var staticsRoot = Path.Combine(new[] { applicationRoot, SiteConfiguration.GetLocalStaticsFolder(loc.LocalizationId) });
                var path = Path.Combine(new[] { staticsRoot, loc.Path.ToCombinePath(), SiteConfiguration.SystemFolder, @"resources\_all.json" });
                if (File.Exists(path))
                {
                    //The _all.json file contains a list of all other resources files to load
                    Log.Debug("Loading resource bootstrap file : '{0}'", path);
                    var bootstrapJson = Json.Decode(File.ReadAllText(path));
                    foreach (string file in bootstrapJson.files)
                    {
                        var type = file.Substring(file.LastIndexOf("/", StringComparison.Ordinal) + 1);
                        type = type.Substring(0, type.LastIndexOf(".", StringComparison.Ordinal)).ToLower();
                        var filePath = Path.Combine(staticsRoot, file.ToCombinePath());
                        if (File.Exists(filePath))
                        {
                            Log.Debug("Loading resources from file: {0}", filePath);
                            foreach (var item in GetResourcesFromFile(filePath))
                            {
                                //we ensure resource key uniqueness by adding the type (which comes from the filename)
                                resources.Add(String.Format("{0}.{1}", type, item.Key), item.Value);
                            }
                        }
                        else
                        {
                            Log.Error("Resource file: {0} does not exist - skipping", filePath);
                        }
                    }
                    _resources.Add(loc.LocalizationId, resources);
                }
                else
                {
                    Log.Warn("Localization resource bootstrap file: {0} does not exist - skipping this localization", path);
                }
            }
        }

        private static Dictionary<string, object> GetResourcesFromFile(string filePath)
        {
            return new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(filePath));
        }
    }

    internal sealed class ResourceReader : IResourceReader
    {
        private readonly IDictionary _dictionary;

        public ResourceReader(IDictionary resources)
        {
            _dictionary = resources;
        }

        public void Close() { }

        public IDictionaryEnumerator GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        public void Dispose() { }
    }
}
