using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DD4T.ContentModel.Contracts.Providers
{
    public enum ProviderVersion
    {
        Tridion2009,
        Tridion2011,
        Tridion2011sp1,
        Tridion2013,
        Tridion2013sp1,
        Undefined
    }

    /// <summary>
    /// Contains information about all supported provider versions
    /// </summary>
    /// <remarks>
    /// To add support for another provider, do the following:
    /// - Add the new provider to the enum ProviderVersion
    /// - Add the ProviderVersion to the dictionary mapping to the full name of the assembly
    /// - If necessary, make the new provider the default
    /// </remarks>
    public static class ProviderAssemblyNames
    {
        public static ProviderVersion DefaultProviderVersion = ProviderVersion.Tridion2013sp1;
        private static Dictionary<ProviderVersion, string> _dictionary = null;
        private static Dictionary<ProviderVersion, string> Dictionary
        {
            get
            {
                if (_dictionary == null)
                {
                    _dictionary = new Dictionary<ProviderVersion, string>();
                    _dictionary[ProviderVersion.Tridion2009] = "DD4T.Providers.SDLTridion2009";
                    _dictionary[ProviderVersion.Tridion2011] = "DD4T.Providers.SDLTridion2011";
                    _dictionary[ProviderVersion.Tridion2011sp1] = "DD4T.Providers.SDLTridion2011sp1";
                    _dictionary[ProviderVersion.Tridion2013] = "DD4T.Providers.SDLTridion2013";
                    _dictionary[ProviderVersion.Tridion2013sp1] = "DD4T.Providers.SDLTridion2013sp1";
                }
                return _dictionary;
            }
        }
        public static string GetProviderClassName(ProviderVersion version)
        {
            return Dictionary[version];
        }

    }
}
