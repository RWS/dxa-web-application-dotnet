using System;
using System.Collections.Generic;

namespace Sdl.Tridion.Context
{
    public class OsClaims : ContextClaims
    {
        public OsClaims(Dictionary<Uri, object> claims) : base(claims)
        {
        }

        public OsClaims()
        {
        }

        public string Model { get { return GetStringValue(ClaimUris.UriOsModel); } }
        public string Variant { get { return GetStringValue(ClaimUris.UriOsVariant); } }
        public string Vendor { get { return GetStringValue(ClaimUris.UriOsVendor); } }
        public string Version { get { return GetStringValue(ClaimUris.UriOsVersion); } }
    }
}
