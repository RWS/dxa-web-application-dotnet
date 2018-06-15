using System;
using System.Collections;
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
        private readonly ILocalization _localization;
        private IDictionary<string, IDictionary<string, string>> _config;
        private IDictionary _resources;

        private readonly object _loadLock = new object();

        public LocalizationResources(ILocalization localization)
        {
            _localization = localization;
        }

        /// <summary>
        /// Force reload of resources.
        /// </summary>
        public virtual void Reload()
        {
            _resources = null;
            _config = null;          
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

                IDictionary<string, string> configSection;
                if (_config == null || !_config.TryGetValue(sectionName, out configSection))
                {
                    configSection = LoadConfigSection(sectionName);
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
        public virtual IDictionary GetResources(string sectionName = null)
        {
            if (_resources != null) return _resources;
            lock (_loadLock)
            {
                if (_resources == null)
                {
                    LoadResources();
                }
            }
            return _resources;
        }   

        protected void LoadResources()
        {
            using (new Tracer(this))
            {
                try
                {
                    BootstrapData resourcesData = null;
                    _localization.LoadStaticContentItem("resources/_all.json", ref resourcesData);

                    var newResources = new Hashtable();
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
                            newResources.Add($"{type}.{resource.Key}", resource.Value);
                        }
                    }

                    _resources = newResources;
                }
                catch (Exception)
                {
                    Log.Warn("Failed to open 'resources/_all.json'");
                    _resources = new Dictionary<string, object>();
                }
            }
        }

        protected IDictionary<string, string> LoadConfigSection(string sectionName)
        {
            using (new Tracer(sectionName, this))
            {
                lock (_loadLock)
                {
                    if (_config == null)
                    {
                        _config = new Dictionary<string, IDictionary<string, string>>();
                    }

                    string configItemUrl = $"{_localization.Path}/{SiteConfiguration.SystemFolder}/config/{sectionName}.json";
                    string configJson = SiteConfiguration.ContentProvider.GetStaticContentItem(configItemUrl, _localization).GetText();
                    IDictionary<string, string> configSection = JsonConvert.DeserializeObject<Dictionary<string, string>>(configJson);
                    _config[sectionName] = configSection;
                    return configSection;
                }
            }
        }      
    }
}
