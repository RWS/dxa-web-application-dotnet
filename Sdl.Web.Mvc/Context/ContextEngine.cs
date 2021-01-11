using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Mvc.Configuration;

namespace Sdl.Web.Mvc.Context
{
    /// <summary>
    /// Provides access to the context claims as provided by the Context Engine.
    /// </summary>
    /// <remarks>
    /// Based on https://github.com/NunoLinhares/TridionContextEngineWrapper/tree/master/Sdl.Tridion.Context
    /// </remarks>
    public class ContextEngine
    {
        private const string DeviceFamiliesFileName = "device-families.xml";

        private readonly IDictionary<string, object> _claims = new Dictionary<string, object>();
        private readonly IDictionary<Type, ContextClaims> _stronglyTypedClaims = new Dictionary<Type, ContextClaims>();
        private string _deviceFamily;
        private static XDocument _deviceFamiliesDoc;

        /// <summary>
        /// Initializes a new <see cref="ContextEngine"/> instance.
        /// </summary>
        /// <remarks><see cref="ContextEngine"/> should not be constructed directly, but through <see cref="Sdl.Web.Mvc.Configuration.WebRequestContext.ContextEngine"/>.</remarks>
        internal ContextEngine()
        {
            using (new Tracer())
            {
                // For now, we get all context claims (for all aspects) in one go:
                IContextClaimsProvider contextClaimsProvider = SiteConfiguration.ContextClaimsProvider;
                if (contextClaimsProvider == null) return;
                _claims = contextClaimsProvider.GetContextClaims(null, WebRequestContext.Localization);

                if (Log.Logger.IsDebugEnabled)
                {
                    Log.Debug("Obtained {0} Context Claims from {1}:", _claims.Count, contextClaimsProvider.GetType().Name);
                    foreach (KeyValuePair<string, object> claim in _claims)
                    {
                        Log.Debug("\t{0} = {1}", claim.Key, FormatClaimValue(claim.Value));
                    }
                }
            }
        }

        private static string FormatClaimValue(object claimValue)
        {
            if (claimValue == null)
            {
                return "(null)";
            }

            if (claimValue is string)
            {
                return $"\"{claimValue}\"";
            }

            if (claimValue is IEnumerable)
            {
                string[] items = (from object item in (IEnumerable) claimValue select FormatClaimValue(item)).ToArray();
                return "{" + string.Join(", ", items) + "}";
            }

            return claimValue.ToString();
        }

        /// <summary>
        /// Gets strongly typed claims of a given type (for a specific aspect).
        /// </summary>
        /// <typeparam name="T">
        /// The strongly typed claims class. Must be a subclass of <see cref="ContextClaims"/>. 
        /// For example: <see cref="DeviceClaims"/>, <see cref="BrowserClaims"/> or <see cref="OperatingSystemClaims"/>.
        /// </typeparam>
        /// <returns>An instance of the strongly typed claims class which can be used to access context claims for a specific aspect.</returns>
        public T GetClaims<T>() 
            where T: ContextClaims, new()
        {
            using (new Tracer(typeof(T)))
            {
                ContextClaims stronglyTypedClaims;
                if (!_stronglyTypedClaims.TryGetValue(typeof(T), out stronglyTypedClaims))
                {
                    stronglyTypedClaims = new T();
                    stronglyTypedClaims.SetClaims(_claims);
                    _stronglyTypedClaims.Add(typeof(T), stronglyTypedClaims);
                }
                return (T)stronglyTypedClaims;
            }
        }

        /// <summary>
        /// Gets all the possible device families specified in the device-families.xml.
        /// </summary>
        /// <returns>A list of all possible device family names.</returns>
        public static IList<string> DeviceFamilies
        {
            get
            {
                using (new Tracer())
                {
                    List<string> result = new List<string>();
                    string deviceFamiliesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", DeviceFamiliesFileName);
                    if (File.Exists(deviceFamiliesPath))
                    {
                        try
                        {
                            _deviceFamiliesDoc = XDocument.Load(deviceFamiliesPath);
                            foreach (XElement deviceFamilyElement in _deviceFamiliesDoc.Descendants("devicefamily"))
                            {
                                string deviceFamilyName = deviceFamilyElement.Attribute("name").Value;
                                result.Add(deviceFamilyName);
                                Log.Debug("Found Device Family '{0}'.", deviceFamilyName);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Unable to parse Device Families file '{0}'.", deviceFamiliesPath);
                            Log.Error(ex);
                            _deviceFamiliesDoc = null;
                        }
                    }
                    else
                    {
                        Log.Error("The Device Families file at '{0}' could not be found.", deviceFamiliesPath);
                    }
                    return result;
                }
            }
        }

        /// <summary>
        /// Gets the device family (an aggregated device claim determined from other context claims).
        /// </summary>
        /// <returns>A string representing the device family.</returns>
        public string DeviceFamily
        {
            get
            {
                if (_deviceFamily != null) return _deviceFamily;
                var provider = SiteConfiguration.ContextClaimsProvider;
                if (provider == null) return _deviceFamily;
                _deviceFamily = provider.GetDeviceFamily();
                if (_deviceFamily != null) return _deviceFamily;
                if (_deviceFamiliesDoc == null)
                {
                    Log.Warn("Device Families file '{0}' was not loaded properly; using defaults.", DeviceFamiliesFileName);

                    // Defaults
                    DeviceClaims device = GetClaims<DeviceClaims>();
                    if (!device.IsMobile && !device.IsTablet) _deviceFamily = "desktop";
                    if (device.IsTablet) _deviceFamily = "tablet";
                    if (device.IsMobile && !device.IsTablet)
                    {
                        _deviceFamily = device.DisplayWidth > 319 ? "smartphone" : "featurephone";
                    }
                }
                else
                {
                    _deviceFamily = DetermineDeviceFamily();
                }
                return _deviceFamily;
            }
        }

        private string DetermineDeviceFamily()
        {
            using (new Tracer())
            {
                foreach (XElement deviceFamilyElement in _deviceFamiliesDoc.Descendants("devicefamily"))
                {
                    string deviceFamily = deviceFamilyElement.Attribute("name").Value;
                    bool inFamily = true;
                    try
                    {
                        foreach (XElement conditionElement in deviceFamilyElement.Descendants("condition"))
                        {
                            string contextClaimName = conditionElement.Attribute("context-claim").Value;
                            if (!HasContextClaim(contextClaimName)) continue;
                            string expectedValue = conditionElement.Attribute("value").Value;

                            if (expectedValue.StartsWith("<"))
                            {
                                int value = Convert.ToInt32(expectedValue.Substring(1));
                                int claimValue = GetContextClaim<int>(contextClaimName);
                                if (claimValue >= value)
                                    inFamily = false;
                            }
                            else if (expectedValue.StartsWith(">"))
                            {
                                int value = Convert.ToInt32(expectedValue.Substring(1));
                                int claimValue = GetContextClaim<int>(contextClaimName);
                                if (claimValue <= value)
                                    inFamily = false;
                            }
                            else
                            {
                                string stringClaimValue = GetContextClaim<string>(contextClaimName);
                                if (!stringClaimValue.Equals(expectedValue, StringComparison.InvariantCultureIgnoreCase))
                                    inFamily = false; 
                            }
                        }
                        if (inFamily)
                        {
                            Log.Debug("Determined Device Family: '{0}'", deviceFamily);
                            return deviceFamily;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Unable to evaluate Device Family '{0}'", deviceFamily);
                        Log.Warn(ex.ToString());
                        return string.Empty;
                    }
                }

                Log.Debug("None of the Device Families matched.");
                return string.Empty;
            }
        }

        private bool HasContextClaim(string name) => _claims.ContainsKey(name);

        private T GetContextClaim<T>(string name)
        {
            object claimValue;
            if (!_claims.TryGetValue(name, out claimValue))
            {
                throw new DxaException($"Context Claim '{name}' not found.");
            }

            return ContextClaims.CastValue<T>(claimValue);
        }
    }
}
