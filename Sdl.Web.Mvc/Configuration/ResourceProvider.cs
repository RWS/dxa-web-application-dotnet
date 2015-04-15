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
            return Resources(WebRequestContext.Localization.Path);
        }

        public IDictionary Resources(string localization)
        {
            if (_resources == null || LastSettingsRefresh < SiteConfiguration.LastSettingsRefresh)
            {
                LoadResources();
            }
            if (!_resources.ContainsKey(localization))
            {
                var ex = new Exception(String.Format("No resources can be found for localization {0}. Check that the localization path is correct and the resources have been published.", localization));
                Log.Error(ex);
                throw ex;
            }
            return _resources[localization];
        }

        private static void LoadResources()
        {
            //We are reading into a static variable, so need to be thread safe
            lock (ResourceLock)
            {
                var applicationRoot = AppDomain.CurrentDomain.BaseDirectory;
                _resources = new Dictionary<string, Dictionary<string, object>>();
                foreach (var loc in SiteConfiguration.Localizations.Values)
                {
                    //Just in case the same localization is in there more than once
                    if (!_resources.ContainsKey(loc.Path))
                    {
                        Log.Debug("Loading resources for localization : '{0}'", loc.Path);
                        var resources = new Dictionary<string, object>();
                        var path = Path.Combine(new[] { applicationRoot, SiteConfiguration.StaticsFolder, loc.Path.ToCombinePath(), SiteConfiguration.SystemFolder, @"resources\_all.json" });
                        if (File.Exists(path))
                        {
                            //The _all.json file contains a list of all other resources files to load
                            Log.Debug("Loading resource bootstrap file : '{0}'", path);
                            var bootstrapJson = Json.Decode(File.ReadAllText(path));
                            foreach (string file in bootstrapJson.files)
                            {
                                var type = file.Substring(file.LastIndexOf("/", StringComparison.Ordinal) + 1);
                                type = type.Substring(0, type.LastIndexOf(".", StringComparison.Ordinal)).ToLower();
                                var filePath = Path.Combine(applicationRoot, SiteConfiguration.StaticsFolder, file.ToCombinePath());
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
                            _resources.Add(loc.Path, resources);
                        }
                        else
                        {
                            Log.Warn("Localization resource bootstrap file: {0} does not exist - skipping this localization", path);
                        }
                    }
                }
            }
            LastSettingsRefresh = DateTime.Now;
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
