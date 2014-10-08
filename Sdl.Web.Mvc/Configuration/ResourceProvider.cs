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
        private static Dictionary<string, Dictionary<string, object>> _resources = new Dictionary<string,Dictionary<string,object>>();
        private static readonly object ResourceLock = new object();
        private static readonly object RefreshLock = new object();
        private static Dictionary<string, DateTime> _localizationLastRefreshes = new Dictionary<string, DateTime>();
        
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
            var key = localization.LocalizationId;
            //Load resources if they are not already loaded, or if they are out of date and need refreshing
            if (!_resources.ContainsKey(key) || (_localizationLastRefreshes.ContainsKey(key) && _localizationLastRefreshes[key] < localization.LastSettingsRefresh))
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
            if (_localizationLastRefreshes.ContainsKey(loc.LocalizationId))
            {
                _localizationLastRefreshes[loc.LocalizationId] = DateTime.Now;
            }
            else
            {
                _localizationLastRefreshes.Add(loc.LocalizationId, DateTime.Now);
            }
            var key = loc.LocalizationId;
            {
                Log.Debug("Loading resources for localization : '{0}'", loc.GetBaseUrl());
                var resources = new Dictionary<string, object>();
                var url = Path.Combine(loc.Path.ToCombinePath(), SiteConfiguration.SystemFolder, @"resources\_all.json").Replace("\\","/");
                var jsonData = SiteConfiguration.StaticFileManager.Serialize(url, loc, true);
                if (jsonData!=null)
                {
                    //The _all.json file contains a list of all other resources files to load
                    var bootstrapJson = Json.Decode(jsonData);
                    foreach (string resourceUrl in bootstrapJson.files)
                    {
                        var type = resourceUrl.Substring(resourceUrl.LastIndexOf("/", StringComparison.Ordinal) + 1);
                        type = type.Substring(0, type.LastIndexOf(".", StringComparison.Ordinal)).ToLower();
                        jsonData = SiteConfiguration.StaticFileManager.Serialize(resourceUrl, loc, true);
                        if (jsonData!=null)
                        {
                            Log.Debug("Loading resources from file: {0}", resourceUrl);
                            foreach (var item in GetResourcesFromFile(jsonData))
                            {
                                //we ensure resource key uniqueness by adding the type (which comes from the filename)
                                resources.Add(String.Format("{0}.{1}", type, item.Key), item.Value);
                            }
                        }
                        else
                        {
                            Log.Error("Resource file: {0} does not exist for localization {1} - skipping", resourceUrl, key);
                        }
                    }
                    if (_resources.ContainsKey(key))
                    {
                        _resources[key] = resources;
                    }
                    else
                    {
                        _resources.Add(key, resources);
                    }
                }
                else
                {
                    Log.Error("Localization resource bootstrap file: {0} does not exist for localization {1}. Check that the Publish Settings page has been published in this publication.", url, loc.LocalizationId);
                }

            }
        }

        private static Dictionary<string, object> GetResourcesFromFile(string jsonData)
        {
            return new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(jsonData);
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
