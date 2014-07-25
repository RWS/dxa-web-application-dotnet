using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Tridion.ContentDelivery.AmbientData;

namespace Sdl.Web.Tridion.Context
{
    /// <summary>
    /// Based on https://github.com/NunoLinhares/TridionContextEngineWrapper/tree/master/Sdl.Tridion.Context
    /// </summary>
    public class ContextEngine
    {
        private readonly BrowserClaims _browser;
        private readonly DeviceClaims _device;
        private readonly OsClaims _os;

        public ContextEngine(Dictionary<Uri, object> claims)
        {
            _browser = new BrowserClaims(claims);
            _device = new DeviceClaims(claims);
            _os = new OsClaims(claims);
        }

        public ContextEngine()
        {
            Dictionary<Uri, object> claims = (Dictionary<Uri, object>)AmbientDataContext.CurrentClaimStore.GetAll();
            _browser = new BrowserClaims(claims);
            _device = new DeviceClaims(claims);
            _os = new OsClaims(claims);
        }

        public BrowserClaims Browser { get { return _browser; } }
        public DeviceClaims Device { get { return _device; } }
        public OsClaims Os { get { return _os; } }

        public string DeviceFamily
        {
            get { return GetDeviceFamily(); }
        }

        private string _deviceFamily;

        private string GetDeviceFamily()
        {
            // check configuration
            // evaluate conditions from top to bottom
            // return current device family

            // if families.xml does not exist will use defaults

            // YUCK

            if (_deviceFamily != null) return _deviceFamily;

            // Could use: string path = VirtualPathUtility.ToAbsolute("~/App_Data/somedata.xml");
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "Families.xml");
            if (File.Exists(path))
            {
                XDocument families = XDocument.Load(path);
                foreach (var i in families.Descendants("devicefamily"))
                {
                    string family = i.Attribute("name").Value;
                    bool inFamily = true;
                    foreach (var c in i.Descendants("condition"))
                    {
                        Uri uri = new Uri(c.Attribute("uri").Value);
                        string expectedValue = c.Attribute("value").Value;

                        if (expectedValue.StartsWith("<"))
                        {
                            int value = Convert.ToInt32(expectedValue.Replace("<", String.Empty));
                            int claimValue = Convert.ToInt32(AmbientDataContext.CurrentClaimStore.Get<string>(uri));
                            if (claimValue >= value)
                                inFamily = false;
                        }
                        else if (expectedValue.StartsWith(">"))
                        {
                            int value = Convert.ToInt32(expectedValue.Replace(">", String.Empty));
                            int claimValue = Convert.ToInt32(AmbientDataContext.CurrentClaimStore.Get<string>(uri));
                            if (claimValue <= value)
                                inFamily = false;
                        }
                        else
                        {
                            string stringClaimValue = AmbientDataContext.CurrentClaimStore.Get<string>(uri);
                            if (!stringClaimValue.Equals(expectedValue))
                                inFamily = false; // move on to next family
                        }
                    }
                    if (inFamily)
                    {
                        _deviceFamily = family;
                        break;
                    }
                    // Need to evaluate if all conditions are true.
                }
            }
            else
            {
                // Defaults
                if (!Device.IsMobile && !Device.IsTablet) _deviceFamily = "desktop";
                if (Device.IsTablet) _deviceFamily = "tablet";
                if (Device.IsMobile && !Device.IsTablet)
                {
                    _deviceFamily = Device.DisplayWidth > 319 ? "smartphone" : "featurephone";
                }
            }

            return _deviceFamily;
        }
    }
}
