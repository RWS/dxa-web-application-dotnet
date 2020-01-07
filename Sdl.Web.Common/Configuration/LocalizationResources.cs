using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.DataModel.Configuration;

namespace Sdl.Web.Common.Configuration
{   
    /// <summary>
    /// Localization Resources
    /// </summary>
    public class LocalizationResources : ILocalizationResources
    {
        private readonly Localization _localization;
        private ConcurrentDictionary<string, IDictionary<string, string>> _config = new ConcurrentDictionary<string, IDictionary<string, string>>();
        private IDictionary _resources;

        private readonly object _loadLock = new object();

        public LocalizationResources(Localization localization)
        {
            _localization = localization;           
        }

        /// <summary>
        /// Force reload of resources.
        /// </summary>
        public virtual void Reload()
        {
            lock (_loadLock)
            {
                _resources = LoadResources();
            }
            
            _config.Clear();
        }

        /// <summary>
        /// Gets a configuration value with a given key.
        /// </summary>
        /// <param name="key">The configuration key, in the format section.name.</param>
        /// <returns>The configuration value.</returns>
        public virtual string GetConfigValue(string key)
        {
            using (new Tracer(key, this))
            {
                string[] keyParts = key.Split('.');
                if (keyParts.Length < 2)
                {
                    throw new DxaException($"Configuration key '{key}' is in the wrong format. It must be in the format [section].[key]");
                }

                //We actually allow more than one . in the key (for example core.schemas.article) in this case the section
                //is the part up to the last dot and the key is the part after it.
                string sectionName = key.Substring(0, key.LastIndexOf(".", StringComparison.Ordinal));
                string propertyName = keyParts[keyParts.Length - 1];

                var configSection = _config.GetOrAdd(sectionName, (k) => LoadConfigSection(k));
                if (configSection == null)
                {
                    Log.Debug("Configuration section '{0}' does not exist for Localization [{1}]. GetConfigValue returns null.", sectionName, this);
                    return null;
                }

                string configValue;
                if (!configSection.TryGetValue(propertyName, out configValue))
                {
                    Log.Debug("Configuration key '{0}' does not exist in section '{1}' for Localization [{2}]. GetConfigValue returns null.", propertyName, sectionName, this);
                }

                return configValue;
            }
        }

        /// <summary>
        /// Gets resources.
        /// </summary>
        /// <param name="sectionName">Optional name of the section for which to get resource. If not specified (or <c>null</c>), all resources are obtained.</param>
        public virtual IDictionary GetResources(string sectionName = null) => _resources;

        protected IDictionary LoadResources()
        {
            using (new Tracer(this))
            {
                try
                {
                    BootstrapData resourcesData = null;
                    _localization.LoadStaticContentItem("resources/_all.json", ref resourcesData);

                    var allResources = new Hashtable();
                    foreach (string staticContentItemUrl in resourcesData.Files)
                    {
                        string type =
                            staticContentItemUrl.Substring(
                                staticContentItemUrl.LastIndexOf("/", StringComparison.Ordinal) + 1);
                        type = type.Substring(0, type.LastIndexOf(".", StringComparison.Ordinal)).ToLower();
                        string resourcesJson =
                            SiteConfiguration.ContentProvider.GetStaticContentItem(staticContentItemUrl, _localization)
                                .GetText();
                        IDictionary<string, object> resources =
                            JsonConvert.DeserializeObject<Dictionary<string, object>>(resourcesJson);
                        foreach (KeyValuePair<string, object> resource in resources)
                        {
                            //we ensure resource key uniqueness by adding the type (which comes from the filename)
                            allResources.Add($"{type}.{resource.Key}", resource.Value);
                        }
                    }

                    return allResources;
                }
                catch (Exception)
                {
                    Log.Warn("Failed to open 'resources/_all.json'");
                    return new ConcurrentDictionary<string, object>();
                }
            }
        }

        protected IDictionary<string, string> LoadConfigSection(string sectionName)
        {
            using (new Tracer(sectionName, this))
            {
                string configItemUrl = $"{_localization.Path}/{SiteConfiguration.SystemFolder}/config/{sectionName}.json";
                string configJson = SiteConfiguration.ContentProvider.GetStaticContentItem(configItemUrl, _localization).GetText();
                IDictionary<string, string> configSection = JsonConvert.DeserializeObject<Dictionary<string, string>>(configJson);
                return configSection;
            }
        }      
    }
}
