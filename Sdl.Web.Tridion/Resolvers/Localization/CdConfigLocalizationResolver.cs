using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;

namespace Sdl.Web.Tridion
{
    /// <summary>
    /// Localization Resolver that reads the URL to Publication mapping from cd_dynamic_conf.xml
    /// </summary>
    public class CdConfigLocalizationResolver : LocalizationResolver
    {
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
                        if (!KnownLocalizations.TryGetValue(publicationId, out localization))
                        {
                            localization = new Localization
                            {
                                Id = publicationId,
                                Path = hostElement.Attribute("Path").Value
                            };
                            KnownLocalizations.Add(publicationId, localization);
                        }
                        _urlToLocalizationMapping.Add(new KeyValuePair<Uri, Localization>(baseUrl, localization));
                    }
                }
            }
        }

        #region ILocalizationResolver Members
        /// <summary>
        /// Resolves a matching <see cref="ILocalization"/> for a given URL.
        /// </summary>
        /// <param name="url">The URL to resolve.</param>
        /// <returns>A <see cref="ILocalization"/> instance which base URL matches that of the given URL.</returns>
        /// <exception cref="DxaUnknownLocalizationException">If no matching Localization can be found.</exception>
        public override Localization ResolveLocalization(Uri url)
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
                    throw new DxaUnknownLocalizationException($"No matching Localization found for URL '{url}'");
                }

                result.EnsureInitialized();
                return result;
            }
        }
        #endregion

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
