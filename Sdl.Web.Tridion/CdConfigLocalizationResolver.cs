using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;

namespace Sdl.Web.Tridion
{
    /// <summary>
    /// Localization Resolver that reads the URL to Publication mapping from cd_dynamic_conf.xml
    /// </summary>
    public class CdConfigLocalizationResolver : ILocalizationResolver
    {
        private readonly IDictionary<string, Localization> _knownLocalizations = new Dictionary<string, Localization>();
        private readonly IList<KeyValuePair<Uri, Localization>> _urlToLocalizationMapping = new List<KeyValuePair<Uri, Localization>>();

        /// <summary>
        /// Initializes a new <see cref="CdConfigLocalizationResolver"/> instance.
        /// </summary>
        public CdConfigLocalizationResolver()
        {
            using (new Tracer())
            {
                string cdDynamicConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"bin\config\cd_dynamic_conf.xml");
                XDocument cdDynamicConfigDoc = XDocument.Load(cdDynamicConfigPath);

                // sorting Publications by Path in decending order so default Path ("/" or "") is last in list
                // using Path of first Host element found in a Publication, assuming the Paths of all of these Host elements will be equal
                XElement publicationsElement = cdDynamicConfigDoc.Descendants("Publications").First();
                IOrderedEnumerable<XElement> publicationElements = publicationsElement.Elements("Publication").OrderByDescending(e => e.Element("Host").Attribute("Path").Value);
                foreach (XElement publicationElement in publicationElements)
                {
                    string publicationId = publicationElement.Attribute("Id").Value;

                    // there could be multiple Host elements per Publication, add them all
                    foreach (XElement hostElement in publicationElement.Elements("Host"))
                    {
                        Uri baseUrl = GetBaseUrl(hostElement);
                        Localization localization;
                        if (!_knownLocalizations.TryGetValue(publicationId, out localization))
                        {
                            localization = new Localization
                            {
                                LocalizationId = publicationId,
                                Path = hostElement.Attribute("Path").Value
                            };
                            _knownLocalizations.Add(publicationId, localization);
                        }
                        _urlToLocalizationMapping.Add(new KeyValuePair<Uri, Localization>(baseUrl, localization));
                    }
                }
            }
        }

        #region ILocalizationResolver Members
        /// <summary>
        /// Resolves a matching <see cref="Localization"/> for a given URL.
        /// </summary>
        /// <param name="url">The URL to resolve.</param>
        /// <returns>A <see cref="Localization"/> instance which base URL matches that of the given URL.</returns>
        /// <exception cref="DxaUnknownLocalizationException">If no matching Localization can be found.</exception>
        public Localization ResolveLocalization(Uri url)
        {
            using (new Tracer(url))
            {
                Localization result;
                try
                {
                    result = _urlToLocalizationMapping.First(mapping => MatchesBaseUrl(url, mapping.Key)).Value;
                }
                catch (Exception)
                {
                    throw new DxaUnknownLocalizationException(string.Format("No matching Localization found for URL '{0}'", url));
                }

                result.EnsureInitialized();
                return result;
            }
        }

        /// <summary>
        /// Gets a <see cref="Localization"/> by its identifier.
        /// </summary>
        /// <param name="localizationId">The Localization identifier.</param>
        /// <returns>A <see cref="Localization"/> instance with the given identifier.</returns>
        /// <exception cref="DxaUnknownLocalizationException">If no matching Localization can be found.</exception>
        public Localization GetLocalization(string localizationId)
        {
            using (new Tracer(localizationId))
            {
                Localization result;
                if (!_knownLocalizations.TryGetValue(localizationId, out result))
                {
                    throw new DxaUnknownLocalizationException(string.Format("No Localization found with ID '{0}'", localizationId));
                }

                return result;
            }
        }

        #endregion


        private static bool MatchesBaseUrl(Uri url, Uri baseUrl)
        {
            return
                (url.Scheme == baseUrl.Scheme) &&
                (url.Host == baseUrl.Host) &&
                (url.Port == baseUrl.Port) &&
                url.AbsolutePath.StartsWith(baseUrl.AbsolutePath, StringComparison.InvariantCultureIgnoreCase);
        }


        private static Uri GetBaseUrl(XElement hostElement)
        {
            UriBuilder baseUrlBuilder = new UriBuilder
            {
                Scheme = hostElement.Attribute("Protocol").Value,
                Host = hostElement.Attribute("Domain").Value,
                Port = Convert.ToInt32(hostElement.Attribute("Port").Value),
                Path = hostElement.Attribute("Path").Value
            };

            return baseUrlBuilder.Uri;
        }

    }
}
