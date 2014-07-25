using System;
using System.Collections.Generic;

namespace Sdl.Web.Tridion.Context
{
    /// <summary>
    /// Based on https://github.com/NunoLinhares/TridionContextEngineWrapper/tree/master/Sdl.Tridion.Context
    /// </summary>
    public class DeviceClaims : ContextClaims
    {        
        public DeviceClaims(Dictionary<Uri, object> claims) : base(claims)
        {
        }

        public DeviceClaims()
        {
        }

        public int DisplayHeight { get { return GetIntValue(ClaimUris.UriDeviceDisplayHeight); } }
        public int DisplayWidth { get { return GetIntValue(ClaimUris.UriDeviceDisplayWidth); } }
        public bool IsMobile { get { return GetBooleanValue(ClaimUris.UriMobile); } }
        public string Model { get { return GetStringValue(ClaimUris.UriDeviceModel); } }
        public int PixelDensity{get { return GetIntValue(ClaimUris.UriPixelDensity); }}
        public double PixelRatio { get { return GetDoubleValue(ClaimUris.UriPixelRatio); }}
        public bool IsRobot { get { return GetBooleanValue(ClaimUris.UriRobot); } }
        public bool IsTablet { get { return GetBooleanValue(ClaimUris.UriTablet); } }
        public string Variant { get { return GetStringValue(ClaimUris.UriDeviceVariant); } }
        public string Vendor { get { return GetStringValue(ClaimUris.UriDeviceVendor); } }
        public string Version { get { return GetStringValue(ClaimUris.UriDeviceVendor); } }
    }
}
