using DD4T.ContentModel.Contracts.Configuration;
using DD4T.ContentModel.Contracts.Providers;
using System;
using System.Collections.Concurrent;
using System.Configuration;

namespace DD4T.Utils
{
    public class DD4TConfiguration : IDD4TConfiguration
    {
        public static readonly string DefaultWelcomeFile = "default.html";
        public static readonly string DefaultDataFormat = "json";
        public static readonly int DefaultNumberOfRetriesToConnect = 10;
        public static readonly int DefaultSecondsBetweenRetries = 10;

        private int? _defaultCacheSettings;
        private string _resourcePath;
        private bool? _useUriAsAnchor;
        private int? _jmsPort;
        private int? _jmsNumberOfRetriesToConnect;
        private int? _jmsSecondsBetweenRetries;
        private string _jmsHostname;
        private string _binaryFileSystemCachePath;
        private string _jmsTopic;
        int? publicationId;
        private string _activeWebsite;
        private string _selectComponentPresentationByComponentTemplateId;
        private string _selectComponentPresentationByOutputFormat;
        private string _dataFormat;
        private string _contentProviderEndPoint;
        private string _siteMapPath;
        private int? _binaryHandlerCacheExpiration;
        private string _binaryFileExtensions;
        private string _binaryUrlPattern;
        private bool? _includeLastPublishedDate;
        private bool? _showAnchors;
        private bool? _linkToAnchor;
        private bool? _isPreview;
        private bool? _useDefaultViewModels;

        public int PublicationId
        {
            get
            {
                if (publicationId == null)
                {
                    int r = SafeGetConfigSettingAsInt(ConfigurationKeys.PublicationId);
                    if (r == int.MinValue)
                    {
                        publicationId = new int?(0);
                    }
                    else
                    {
                        publicationId = new int?(r);
                    }
                }
                return publicationId.Value;
            }
        }

        private string _welcomeFile;
        public string WelcomeFile
        {
            get
            {
                if (_welcomeFile == null)
                {
                    var configurationValue = SafeGetConfigSettingAsString(ConfigurationKeys.WelcomeFile, ConfigurationKeys.WelcomeFileAlt1);
                    if (string.IsNullOrEmpty(configurationValue))
                    {
                        _welcomeFile = DefaultWelcomeFile;
                    }
                    else
                    {
                        _welcomeFile = configurationValue;
                    }
                }
                return _welcomeFile;
            }
        }


        public string ComponentPresentationController
        {
            get
            {
                return SafeGetConfigSettingAsString(ConfigurationKeys.ComponentPresentationController);
            }
        }

        public string ComponentPresentationAction
        {
            get
            {
                return SafeGetConfigSettingAsString(ConfigurationKeys.ComponentPresentationAction, ConfigurationKeys.ComponentPresentationActionAlt1);
            }
        }


        public string ActiveWebsite
        {
            get
            {
                if (_activeWebsite == null)
                {
                    _activeWebsite = SafeGetConfigSettingAsString(ConfigurationKeys.ActiveWebsite, ConfigurationKeys.ActiveWebsiteAlt1);
                }
                return _activeWebsite;
            }
        }

        [Obsolete("Use SelectComponentPresentationByComponentTemplateId instead")]
        public string SelectComponentByComponentTemplateId
        {
            get
            {
                return SelectComponentPresentationByComponentTemplateId;
            }
        }
        public string SelectComponentPresentationByComponentTemplateId
        {
            get
            {
                if (_selectComponentPresentationByComponentTemplateId == null)
                {
                    _selectComponentPresentationByComponentTemplateId = SafeGetConfigSettingAsString(ConfigurationKeys.SelectComponentByComponentTemplateId, ConfigurationKeys.SelectComponentByComponentTemplateIdAlt1);
                }
                return _selectComponentPresentationByComponentTemplateId;
            }
        }

        [Obsolete("Use SelectComponentPresentationByOutputFormat instead")]
        public string SelectComponentByOutputFormat
        {
            get
            {
                return SelectComponentPresentationByOutputFormat;
            }
        }
        public string SelectComponentPresentationByOutputFormat
        {
            get
            {
                if (_selectComponentPresentationByOutputFormat == null)
                {
                    _selectComponentPresentationByOutputFormat = SafeGetConfigSettingAsString(ConfigurationKeys.SelectComponentByOutputFormat, ConfigurationKeys.SelectComponentByOutputFormatAlt1);
                }
                return _selectComponentPresentationByOutputFormat;
            }
        }

        public string DataFormat
        {
            get
            {
                if (_dataFormat == null)
                {
                    var configurationValue = SafeGetConfigSettingAsString(ConfigurationKeys.DataFormat);
                    if (string.IsNullOrEmpty(configurationValue))
                    {
                        _dataFormat = DefaultDataFormat;
                    }
                    else
                    {
                        _dataFormat = configurationValue;
                    }
                }
                return _dataFormat;
            }
        }



        public string ContentProviderEndPoint
        {
            get
            {
                if (_contentProviderEndPoint == null)
                {
                    var configurationvalue = SafeGetConfigSettingAsString(ConfigurationKeys.ContentProviderEndPoint);
                    if (string.IsNullOrEmpty(configurationvalue))
                    {
                        throw new ConfigurationErrorsException(string.Format("Content Provider endpoint not defined. Configure '{0}'.", ConfigurationKeys.ContentProviderEndPoint));
                    }
                    else
                    {
                        _contentProviderEndPoint = configurationvalue;
                    }
                }
                return _contentProviderEndPoint;
            }
        }


        public string SiteMapPath
        {
            get
            {
                if (_siteMapPath == null)
                {
                    var configurationvalue = SafeGetConfigSettingAsString(ConfigurationKeys.SitemapPath, ConfigurationKeys.SitemapPathAlt1);
                    if (string.IsNullOrEmpty(configurationvalue))
                    {
                        throw new ConfigurationErrorsException(string.Format("SiteMapPath not defined. Configure '{0}'.", ConfigurationKeys.SitemapPath));
                    }
                    else
                    {
                        _siteMapPath = configurationvalue;
                    }
                }
                return _siteMapPath;
            }
        }

        public int BinaryHandlerCacheExpiration
        {
            get
            {
                if (_binaryHandlerCacheExpiration == null)
                {
                    _binaryHandlerCacheExpiration = new int?(SafeGetConfigSettingAsInt(ConfigurationKeys.BinaryHandlerCacheExpiration, ConfigurationKeys.BinaryHandlerCacheExpirationAlt1));
                }
                return _binaryHandlerCacheExpiration.Value;
            }
        }


        public string BinaryFileExtensions
        {
            get
            {
                if (_binaryFileExtensions == null)
                {
                    var configurationvalue = SafeGetConfigSettingAsString(ConfigurationKeys.BinaryFileExtensions, ConfigurationKeys.BinaryFileExtensionsAlt1);
                    if (string.IsNullOrEmpty(configurationvalue))
                    {
                        throw new ConfigurationErrorsException(string.Format("BinaryFileExtensions not defined. Configure '{0}'.", ConfigurationKeys.BinaryFileExtensions));
                    }
                    else
                    {
                        _binaryFileExtensions = configurationvalue;
                    }
                }
                return _binaryFileExtensions;
            }
        }


        public string BinaryUrlPattern
        {
            get
            {
                if (_binaryUrlPattern == null)
                {

                    var configurationvalue = SafeGetConfigSettingAsString(ConfigurationKeys.BinaryUrlPattern);
                    if (string.IsNullOrEmpty(configurationvalue))
                    {
                        throw new ConfigurationErrorsException(string.Format("BinaryUrlPattern not defined. Configure '{0}'.", ConfigurationKeys.BinaryUrlPattern));
                    }
                    else
                    {
                        _binaryUrlPattern = configurationvalue;
                    }
                }
                return _binaryUrlPattern;
            }
        }

        public bool IncludeLastPublishedDate
        {
            get
            {
                if (_includeLastPublishedDate == null)
                {
                    _includeLastPublishedDate = new bool?(SafeGetConfigSettingAsBoolean(ConfigurationKeys.IncludeLastPublishedDate));
                }
                return _includeLastPublishedDate.Value;
            }
        }


        public bool IsPreview
        {
            get
            {
                if (_isPreview == null)
                {
                    _isPreview = new bool?(SafeGetConfigSettingAsBoolean(ConfigurationKeys.IsPreview));
                }
                return _isPreview.Value;
            }
        }

        public bool ShowAnchors
        {
            get
            {
                if (_showAnchors == null)
                {
                    _showAnchors = new bool?(SafeGetConfigSettingAsBoolean(ConfigurationKeys.ShowAnchors));
                }
                return _showAnchors.Value;
            }
        }


        public bool LinkToAnchor
        {
            get
            {
                if (_linkToAnchor == null)
                {
                    _linkToAnchor = new bool?(SafeGetConfigSettingAsBoolean(ConfigurationKeys.LinkToAnchor));
                }
                return _linkToAnchor.Value;
            }
        }

        public int DefaultCacheSettings
        {
            get
            {
                if (_defaultCacheSettings == null)
                {
                    _defaultCacheSettings = new int?(SafeGetConfigSettingAsInt(ConfigurationKeys.DefaultCacheSettings));
                }
                return _defaultCacheSettings.Value;
            }
        }

        public string ViewModelKeyField
        {
            get { return SafeGetConfigSettingAsString(ConfigurationKeys.ViewModelKeyFieldName); }
        }
        public string ResourcePath
        {
            get
            {
                if (_resourcePath == null)
                {
                    _resourcePath = SafeGetConfigSettingAsString(ConfigurationKeys.ResourcePath);
                }
                return _resourcePath;
            }
        }


        public bool UseUriAsAnchor
        {
            get
            {
                if (_useUriAsAnchor == null)
                {
                    _useUriAsAnchor = new bool?(SafeGetConfigSettingAsBoolean(ConfigurationKeys.UseUriAsAnchor));
                }
                return _useUriAsAnchor.Value;
            }
        }



        public int JMSPort
        {
            get
            {
                if (_jmsPort == null)
                {
                    _jmsPort = new int?(SafeGetConfigSettingAsInt(ConfigurationKeys.JMSPort));
                }
                return _jmsPort.Value;
            }
        }
        public int JMSNumberOfRetriesToConnect
        {
            get
            {
                if (_jmsNumberOfRetriesToConnect == null)
                {
                    string configurationvalue = SafeGetConfigSettingAsString(ConfigurationKeys.JMSNumberOfRetriesToConnect);
                    if (string.IsNullOrEmpty(configurationvalue))
                    {
                        _jmsNumberOfRetriesToConnect = new int?(DefaultNumberOfRetriesToConnect);
                    }
                    else
                    {
                        _jmsNumberOfRetriesToConnect = new int?(SafeGetConfigSettingAsInt(ConfigurationKeys.JMSNumberOfRetriesToConnect));
                    }
                }
                return _jmsNumberOfRetriesToConnect.Value;
            }
        }
        public int JMSSecondsBetweenRetries
        {
            get
            {
                if (_jmsSecondsBetweenRetries == null)
                {
                    string configurationvalue = SafeGetConfigSettingAsString(ConfigurationKeys.JMSSecondsBetweenRetries);
                    if (string.IsNullOrEmpty(configurationvalue))
                    {
                        _jmsSecondsBetweenRetries = new int?(DefaultSecondsBetweenRetries);
                    }
                    else
                    {
                        _jmsSecondsBetweenRetries = new int?(SafeGetConfigSettingAsInt(ConfigurationKeys.JMSSecondsBetweenRetries));
                    }
                }
                return _jmsSecondsBetweenRetries.Value;
            }
        }

        public string JMSHostname
        {
            get
            {
                if (_jmsHostname == null)
                {
                    _jmsHostname = SafeGetConfigSettingAsString(ConfigurationKeys.JMSHostname);
                }
                return _jmsHostname;
            }
        }

        public string JMSTopic
        {
            get
            {
                if (_jmsTopic == null)
                {
                    _jmsTopic = SafeGetConfigSettingAsString(ConfigurationKeys.JMSTopic);
                }
                return _jmsTopic;
            }
        }

        private ConcurrentDictionary<string, int> _expirationPerRegion = new ConcurrentDictionary<string, int>();
        public int GetExpirationForCacheRegion(string region)
        {
            if (!_expirationPerRegion.ContainsKey(region))
            {
                string cacheSettingePerRegion = string.Format(ConfigurationKeys.CacheSettingsPerRegion, region);
                string configSetting = SafeGetConfigSettingAsString(cacheSettingePerRegion);

                if (string.IsNullOrEmpty(configSetting))
                {
                    _expirationPerRegion.TryAdd(region, DefaultCacheSettings);
                }
                else
                {
                    _expirationPerRegion.TryAdd(region, SafeGetConfigSettingAsInt(cacheSettingePerRegion));
                }
            }
            return _expirationPerRegion[region];
        }

        public ProviderVersion ProviderVersion
        {
            get { throw new NotImplementedException(); }
        }

        public string BinaryFileSystemCachePath
        {
            get
            {
                if (_binaryFileSystemCachePath == null)
                {
                    _binaryFileSystemCachePath = SafeGetConfigSettingAsString(ConfigurationKeys.BinaryFileSystemCachePath);
                }
                return _binaryFileSystemCachePath;
            }
        }

        public bool UseDefaultViewModels
        {
            get
            {
                if (_useDefaultViewModels == null)
                {
                    string setting = SafeGetConfigSettingAsString(ConfigurationKeys.UseDefaultViewModels);
                    if (string.IsNullOrEmpty(setting))
                    {
                        _useDefaultViewModels = true; // the default for this setting is TRUE!
                    }
                    _useDefaultViewModels = setting.ToLower() == "yes" || setting.ToLower() == "true";
                }
                return _useDefaultViewModels.Value;
            }
        }


        #region private methods
        private static string SafeGetConfigSettingAsString(params string[] keys)
        {
            foreach (string key in keys)
            {
                string setting = ConfigurationManager.AppSettings[key];
                if (!string.IsNullOrEmpty(setting))
                    return setting;
            }
            return string.Empty;
        }

        private static int SafeGetConfigSettingAsInt(params string[] keys)
        {
            string setting = SafeGetConfigSettingAsString(keys);
            if (string.IsNullOrEmpty(setting))
                return int.MinValue;
            int i = int.MinValue;
            Int32.TryParse(setting, out i);
            return i;
        }
        private static bool SafeGetConfigSettingAsBoolean(params string[] keys)
        {
            string setting = SafeGetConfigSettingAsString(keys);
            if (string.IsNullOrEmpty(setting))
                return false;
            bool b = false;
            Boolean.TryParse(setting, out b);
            return b;
        }


        #endregion




    }
}
