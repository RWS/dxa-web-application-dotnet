using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Configuration
{
    /// <summary>
    /// Represents configuration that applies to the entire web application.
    /// </summary>
    public static class SiteConfiguration
    {
        public const string VersionRegex = "(v\\d*.\\d*)";
        public const string SystemFolder = "system";
        public const string CoreModuleName = "core";
        public const string StaticsFolder = "BinaryData";
        public const string DefaultVersion = "v1.00";

        //A set of refresh states, keyed by localization id and then type (eg "config", "resources" etc.) 
        private static readonly Dictionary<string, Dictionary<string, DateTime>> _refreshStates = new Dictionary<string, Dictionary<string, DateTime>>();
        //A set of locks to use, one per localization
        private static readonly Dictionary<string, object> _localizationLocks = new Dictionary<string, object>();
        //A global lock
        private static readonly object _lock = new object();

        #region References to "providers"
        /// <summary>
        /// Gets the Content Provider used for obtaining the Page and Entity Models and Static Content.
        /// </summary>
        public static IContentProvider ContentProvider
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the Content Provider used for obtaining the Navigation Models
        /// </summary>.
        public static INavigationProvider NavigationProvider
        {
            get;
            private set;
        }


        /// <summary>
        /// Gets the Context Claims Provider.
        /// </summary>
        public static IContextClaimsProvider ContextClaimsProvider
        {
            get;
            private set;
        }


        /// <summary>
        /// Gets the Link Resolver.
        /// </summary>
        public static ILinkResolver LinkResolver
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the Rich Text Processor.
        /// </summary>
        public static IRichTextProcessor RichTextProcessor
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the Conditional Entity Evaluator.
        /// </summary>
        public static IConditionalEntityEvaluator ConditionalEntityEvaluator
        {
            get;
            private set;
        }


        /// <summary>
        /// Gets the Media helper used for generating responsive markup for images, videos etc.
        /// </summary>
        public static IMediaHelper MediaHelper 
        { 
            get; 
            private set; 
        }


#pragma warning disable 618
        /// <summary>
        /// Gets the Static File Manager used for serializing and accessing static files published from the CMS (config/resources/HTML design assets etc.)
        /// </summary>
        [Obsolete("Deprecated in DXA 1.1. Use ContentProvider.GetStaticContentItem to get static content.")]
        public static IStaticFileManager StaticFileManager
        {
            get; 
            private set;
        }
#pragma warning restore 618

        /// <summary>
        /// Gets the Localization Resolver used for mapping URLs to Localizations.
        /// </summary>
        public static ILocalizationResolver LocalizationResolver
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the providers (Content Provider, Link Resolver, Media Helper, etc.) using dependency injection, i.e. obtained from configuration.
        /// </summary>
        /// <param name="dependencyResolver">The Dependency Resolver used to get implementations for provider interfaces.</param>
        public static void InitializeProviders(IDependencyResolver dependencyResolver)
        {
            using (new Tracer())
            {
                ContentProvider = GetProvider<IContentProvider>(dependencyResolver);
                NavigationProvider = GetProvider<INavigationProvider>(dependencyResolver);
                ContextClaimsProvider = GetProvider<IContextClaimsProvider>(dependencyResolver);
                LinkResolver = GetProvider<ILinkResolver>(dependencyResolver);
                RichTextProcessor = GetProvider<IRichTextProcessor>(dependencyResolver);
                ConditionalEntityEvaluator = GetProvider<IConditionalEntityEvaluator>(dependencyResolver, isOptional: true);
                MediaHelper = GetProvider<IMediaHelper>(dependencyResolver);
                LocalizationResolver = GetProvider<ILocalizationResolver>(dependencyResolver);
#pragma warning disable 618
                StaticFileManager = GetProvider<IStaticFileManager>(dependencyResolver, isOptional: true);
#pragma warning restore 618
            }
        }

        private static T GetProvider<T>(IDependencyResolver dependencyResolver, bool isOptional = false)
            where T: class // interface to be more precise.
        {
            Type interfaceType = typeof(T);
            T provider = (T) dependencyResolver.GetService(interfaceType);
            if (provider == null)
            {
                if (!isOptional)
                {
                    throw new DxaException(String.Format("No implementation type configured for interface {0}. Check your Unity.config.", interfaceType.Name));
                }
                Log.Info("No implementation type configured for optional interface {0}.", interfaceType.Name);
            }
            else
            {
                Log.Info("Using implementation type '{0}' for interface {1}.", provider.GetType().FullName, interfaceType.Name);
            }
            return provider;
        }
        #endregion


        public static string GetPageController()
        {
            return "Page";
        }

        public static string GetPageAction()
        {
            return "Page";
        }

        public static string GetRegionController()
        {
            return "Region";
        }

        public static string GetRegionAction()
        {
            return "Region";
        }

        public static string GetEntityController()
        {
            return "Entity";
        }

        public static string GetEntityAction()
        {
            return "Entity";
        }

        public static string GetDefaultModuleName()
        {
            return "Core";
        }

        /// <summary>
        /// Removes the version number from a URL path for an asset
        /// </summary>
        /// <param name="path">The URL path</param>
        /// <returns>The 'real' path to the asset</returns>
        public static String RemoveVersionFromPath(string path)
        {
            return Regex.Replace(path, SystemFolder + "/" + VersionRegex + "/", delegate
            {
                return SystemFolder + "/";
            });
        }

        /// <summary>
        /// Ensure that a URL is using the path to the given localization
        /// </summary>
        /// <param name="url">The URL to localize</param>
        /// <param name="localization">The localization to use</param>
        /// <returns>A localized URL</returns>
        public static string LocalizeUrl(string url, Localization localization)
        {
            if (!String.IsNullOrEmpty(localization.Path))
            {
                return localization.Path + "/" + url;
            }
            return url;
        }

        /// <summary>
        /// Take a partial URL (so not including protocol, domain, port) and make it full by
        /// Adding the protocol, domain, port etc. from the given localization
        /// </summary>
        public static string MakeFullUrl(string url, Localization loc)
        {
            if (url.StartsWith(loc.Path))
            {
                url = url.Substring(loc.Path.Length);
            }
            return url.StartsWith("http") ? url : loc.GetBaseUrl() + url;
        }
        
        public static string GetLocalStaticsFolder(string localizationId)
        {
            return String.Format("{0}\\{1}", StaticsFolder, localizationId);
        }

        public static string GetLocalStaticsUrl(string localizationId)
        {
            return GetLocalStaticsFolder(localizationId).Replace("\\","/");
        }

        /// <summary>
        /// Generic a GUID
        /// </summary>
        /// <param name="prefix">prefix for the GUID</param>
        /// <returns>Prefixed Unique Identifier</returns>
        public static string GetUniqueId(string prefix)
        {
            return prefix + Guid.NewGuid().ToString("N");
        }
        
        #region Thread Safe Settings Update Helper Methods

        [Obsolete("Deprecated in DXA 1.1. Use the overload that takes a Localization instance.")]
        public static bool CheckSettingsNeedRefresh(string type, string localizationId)
        {
            return CheckSettingsNeedRefresh(type, LocalizationResolver.GetLocalization(localizationId));
        }
        
        public static bool CheckSettingsNeedRefresh(string type, Localization localization) // TODO: Move to class Localization
        {
            Dictionary<string, DateTime> localizationRefreshStates;
            if (!_refreshStates.TryGetValue(localization.LocalizationId, out localizationRefreshStates))
            {
                return false;
            }
            DateTime settingsRefresh;
            if (!localizationRefreshStates.TryGetValue(type, out settingsRefresh))
            {
                return false;
            }
            return settingsRefresh.AddSeconds(1) < localization.LastRefresh;
        }

        public static void ThreadSafeSettingsUpdate<T>(string type, Dictionary<string, T> settings, string localizationId, T value) // TODO
        {
            lock (GetLocalizationLock(localizationId))
            {
                settings[localizationId] = value;
                UpdateRefreshState(localizationId, type);
            }
        }

        private static void UpdateRefreshState(string localizationId, string type) // TODO
        {
            //Update is already done under a localization lock, so we don't need to lock again here
            if (!_refreshStates.ContainsKey(localizationId))
            {
                _refreshStates.Add(localizationId, new Dictionary<string, DateTime>());
            }
            Dictionary<string, DateTime> states = _refreshStates[localizationId];
            if (states.ContainsKey(type))
            {
                states[type] = DateTime.Now;
            }
            else
            {
                states.Add(type, DateTime.Now);
            }
        }

        private static object GetLocalizationLock(string localizationId)
        {
            if (!_localizationLocks.ContainsKey(localizationId))
            {
                lock (_lock)
                {
                    _localizationLocks.Add(localizationId, new object());
                }
            }
            return _localizationLocks[localizationId];
        }

        #endregion


        #region Obsolete 
        /// <summary>
        /// Gets the include Page URLs for a given Page Type and Localization.
        /// </summary>
        /// <param name="pageTypeIdentifier">The Page Type Identifier.</param>
        /// <param name="localization">The Localization</param>
        /// <returns>The URLs of Include Pages</returns>
        /// <remarks>
        /// The concept of Include Pages will be removed in a future version of DXA.
        /// As of DXA 1.1 Include Pages are represented as <see cref="Sdl.Web.Common.Models.PageModel.Regions"/>.
        /// Implementations should avoid using this method directly.
        /// </remarks>
        [Obsolete("Deprecated in DXA 1.3. Use Localization.GetIncludePageUrls instead (avoid using this method in general).")]
        public static IEnumerable<string> GetIncludePageUrls(string pageTypeIdentifier, Localization localization)
        {
            using (new Tracer(pageTypeIdentifier, localization))
            {
                return localization.GetIncludePageUrls(pageTypeIdentifier);
            }
        }

        /// <summary>
        /// A registry of View Path -> View Model Type mappings to enable the correct View Model to be mapped for a given View
        /// </summary>
        [Obsolete("Dropped in DXA 1.1. Use ModelTypeRegistry.GetViewModelType instead.", true)]
        public static Dictionary<string, Type> ViewModelRegistry
        {
            get;
            set;
        }

        /// <summary>
        /// Adds a View->View Model Type mapping to the view model registry
        /// </summary>
        /// <param name="viewData">The View Data used to determine the registry key and model type</param>
        /// <param name="viewPath">The path to the view</param>
        [Obsolete("Method is deprecated in DXA 1.1. Use BaseAreaRegistration.RegisterViewModel instead.")]
        public static void AddViewModelToRegistry(MvcData viewData, string viewPath)
        {
            ModelTypeRegistry.RegisterViewModel(viewData, viewPath);
        }

        [Obsolete("Method is deprecated in DXA 1.1. Use ModelTypeRegistry instead.")]
        public static string GetViewModelRegistryKey(MvcData mvcData)
        {
            return String.Format("{0}:{1}:{2}", mvcData.AreaName, mvcData.ControllerName, mvcData.ViewName);
        }

        [Obsolete("Method is deprecated in DXA 1.1. Use BaseAreaRegistration.RegisterViewModel instead.")]
        public static void AddViewModelToRegistry(MvcData mvcData, Type modelType)
        {
            ModelTypeRegistry.RegisterViewModel(mvcData, modelType);
        }


        /// <summary>
        /// Gets a (localized) configuration setting
        /// </summary>
        /// <param name="key">The configuration key, in the format "section.name" (eg "Environment.CmsUrl")</param>
        /// <param name="localization">The localization to get config for</param>
        /// <returns>The configuration matching the key for the given localization</returns>
        [Obsolete("Deprecated in DXA 1.1 Use Localization.GetConfigValue instead.")]
        public static string GetConfig(string key, Localization localization)
        {
            using (new Tracer(key, localization))
            {
                return localization.GetConfigValue(key);
            }

        }

        [Obsolete("Dropped in DXA 1.1. Use Localization.GetConfigValue instead.", error: true)]
        public static string GetConfig(string key)
        {
            return null;
        }

        [Obsolete("Dropped in DXA 1.1. Use Localization.Refresh instead.", error: true)]
        public static void Refresh(Localization localization = null)
        {
        }


        [Obsolete("Use Localization.IsStaging property of current localization (eg via WebRequestContext.Localization.IsStaging)", true)]
        public static bool IsStaging { get; set; }
        
        [Obsolete("There is no longer the concept of a global default localization. The Localization.IsDefaultLocalization property can help you find if a localization is the default for its site.", true)]
        public static string DefaultLocalization { get; private set; }

        [Obsolete("Configuration should not be access directly, but rather via the GetConfig(string, Localization) method.", true)]
        public static Dictionary<string, Dictionary<string, Dictionary<string, string>>> LocalConfiguration { get; set; }

        [Obsolete("Configuration should not be access directly, but rather via the GetConfig(string, Localization) method. There is also no longer a concept of Global Configuration, all configuration is local to a particular localization.", true)]
        public static Dictionary<string, Dictionary<string, Dictionary<string, string>>> GlobalConfiguration { get; set; }

        [Obsolete("Settings refresh is now applied at a localization level, rather than globally", true)]
        public static DateTime LastSettingsRefresh { get; set; }

        [Obsolete("Use Localization.MediaUrlRegex property of current localization (eg via WebRequestContext.Localization.IsStaging)", true)]
        public static string MediaUrlRegex { get; set; }

        [Obsolete("GetGlobalConfig(string,string) is deprecated, please use GetConfig(string, Localization) by combining the key and module parameters in the format {module.key} (eg core.schemas.article)", true)]
        public static string GetGlobalConfig(string key, string module = CoreModuleName)
        {
            return null;
        }

        [Obsolete("Use Version property of current Localization instead. Eg WebRequestContext.Localization.Version.", true)]
        public static string SiteVersion { get; set; }

        [Obsolete("Configuration is now lazy loaded on demand per localization, so there is no need to call Load.", true)]
        public static void Load(string applicationRoot)
        {
        }

        [Obsolete("Localizations are now loaded on demand in the web application so this is no longer required", true)]
        public static void SetLocalizations(List<Dictionary<string, string>> localizations)
        {
        }

        [Obsolete("Localizations are now loaded on demand in the web application so this is no longer required", true)]
        public static void Initialize(List<Dictionary<string, string>> localizationList)
        {            
        }

        [Obsolete("Localizations are now loaded on demand in the web application so this is no longer available. Use the SiteConfiguration.LocalizationResolver.GetLocalizationByUri or GetLocalizationById methods", true)]
        public static Dictionary<string, Localization> Localizations { get; set; }

        /// <summary>
        /// Gets a XPM region by name.
        /// </summary>
        /// <param name="name">The region name</param>
        /// <param name="loc"></param>
        /// <returns>The XPM region matching the name for the given module</returns>
        [Obsolete("Deprecated in DXA 1.3. Use Localization.GetXpmRegionConfiguration instead.")]
        public static XpmRegion GetXpmRegion(string name, Localization loc)
        {
            using (new Tracer(name, loc))
            {
                return loc.GetXpmRegionConfiguration(name);
            }
        }
        #endregion

    }
}
