using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Tridion.ContentDelivery.AmbientData;

namespace Sdl.Web.Tridion.Context
{
    public class AdfContextClaimsProvider : IContextClaimsProvider
    {
        private const string ContextClaimPrefix = "taf:claim:context:";

        #region IContextClaimsProvider Members

        /// <summary>
        /// Gets the context claims. Either all context claims or for a given aspect name.
        /// </summary>
        /// <param name="aspectName">The aspect name. If <c>null</c> all context claims are returned.</param>
        /// <returns>A dictionary with the claim names in format aspectName.propertyName as keys.</returns>
        public IDictionary<string, object> GetContextClaims(string aspectName)
        {
            using (new Tracer(aspectName))
            {
                string claimNamePrefix = ContextClaimPrefix;
                if (!string.IsNullOrEmpty(aspectName))
                {
                    claimNamePrefix += aspectName + ":";
                }

                IDictionary<string, object> result = new Dictionary<string, object>();
                foreach (KeyValuePair<Uri, object> claim in AmbientDataContext.CurrentClaimStore.GetAll())
                {
                    string claimName = claim.Key.ToString();
                    if (!claimName.StartsWith(claimNamePrefix))
                    {
                        continue;
                    }
                    string propertyName = claimName.Substring(ContextClaimPrefix.Length).Replace(':', '.');
                    result.Add(propertyName, claim.Value);
                }

                return result;
            }
        }

        /// <summary>
        /// Gets the device family (an aggregated device claim determined from other context claims).
        /// </summary>
        /// <returns>A string representing the device family.</returns>
        public string GetDeviceFamily()
        {
            // TODO TSI-789: this functionality overlaps with "Context Expressions".
            using (new Tracer())
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "Families.xml");
                if (!File.Exists(path))
                {
                    return null;
                }

                string result = null;
                XDocument families = XDocument.Load(path);
                foreach (XElement i in families.Descendants("devicefamily"))
                {
                    string family = i.Attribute("name").Value;
                    bool inFamily = true;
                    foreach (XElement c in i.Descendants("condition"))
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
                        result = family;
                        break;
                    }
                    // Need to evaluate if all conditions are true.
                }

                return result;
            }
        }

        #endregion
    }
}
